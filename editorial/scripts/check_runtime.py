"""Compile and run the game's dialogue checks on the current editorial export."""

import argparse
import hashlib
import os
import subprocess
import sys
import tarfile
from pathlib import Path

from corpus import ROOT, read_json, write_json


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def compile_checker(compiler, name, review):
    sources = [ROOT / 'scripts' / f'{name}.cs']
    refs = ['Prompter.dll', 'UnityEngine.dll', 'UnityEngine.CoreModule.dll']
    if name in ('LeonieSourceChecks', 'Fragonard04SourceChecks'):
        sources.append(ROOT / 'scripts/FullContextStoryChecks.cs')
    if name == 'ScriptChecks':
        refs.append('PrompterCompiler.dll')
    exe = review / f'{name}.exe'
    exe.unlink(missing_ok=True)
    subprocess.run([
        str(compiler), '/nologo', '/codepage:65001', '/target:exe', f'/main:{name}',
        f'/out:{exe}', '/reference:System.Web.Extensions.dll',
        *[f'/reference:{review / ref}' for ref in refs],
        *[str(source) for source in sources],
    ], cwd=ROOT, check=True)
    return exe, {source.relative_to(ROOT).as_posix(): sha(source) for source in sources}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--story-scope', help='режим FullContextStoryChecks, например --myriam-only')
    parser.add_argument('--script', nargs=2, type=Path, metavar=('SOURCE', 'RUSSIAN'),
                        help='сравнить структуру двух файлов через PrompterCompiler')
    args = parser.parse_args()
    compiler = Path(os.environ['WINDIR']) / 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
    review = ROOT / 'review'
    if args.script:
        exe, _ = compile_checker(compiler, 'ScriptChecks', review)
        return subprocess.run([str(exe), *[str(p.resolve()) for p in args.script]],
                              cwd=ROOT).returncode

    localization = ROOT / 'localization'
    export = localization / 'reviewed_engine_input.json'
    input_sha256 = sha(export)
    accepted = read_json(localization / 'editorial_validation.json')
    if not accepted['passed'] or accepted['input_sha256'] != input_sha256:
        raise SystemExit('Сначала выполните validate_editorial_input.py для текущего экспорта.')
    with tarfile.open(ROOT / 'runtime-fixtures.tar.gz', 'r:gz') as fixtures:
        fixtures.extractall(ROOT, filter='data')
    scripts = {'FullContextStoryChecks': [args.story_scope] if args.story_scope else []}
    if not args.story_scope:
        scripts.update(LeonieSourceChecks=[], Fragonard04SourceChecks=[
            str(localization / 'fragonard04_followup_routes.json')])
    results = []
    report = localization / ('runtime_scoped_validation.json' if args.story_scope
                             else 'runtime_validation.json')
    write_json(report, dict(passed=False, full_runtime_deferred=True,
                            input_sha256=input_sha256, scope=args.story_scope, results=results))
    for name, extra in scripts.items():
        exe, sources = compile_checker(compiler, name, review)
        log = review / f'{name}_output.txt'
        with log.open('wb') as out:
            result = subprocess.run([str(exe), str(export), *extra], cwd=ROOT,
                                    stdout=out, stderr=subprocess.STDOUT)
        lines = log.read_text(encoding='utf-8-sig').splitlines()
        checks = sum(line.startswith('PASS ') for line in lines)
        passed = result.returncode == 0 and checks > 0
        results.append(dict(checker=name, passed=passed, exit_code=result.returncode,
                            checks=checks, sources=sources, executable_sha256=sha(exe),
                            output_sha256=sha(log)))
        print(f'{name}: {checks} PASS, код возврата {result.returncode}.', flush=True)
        if not passed:
            print('\n'.join(lines[-6:]), file=sys.stderr)
            break
    passed = len(results) == len(scripts) and all(row['passed'] for row in results)
    write_json(report, dict(passed=passed, full_runtime_deferred=not passed or bool(args.story_scope),
                            input_sha256=input_sha256, scope=args.story_scope, results=results))
    return 0 if passed else 1


if __name__ == '__main__':
    raise SystemExit(main())
