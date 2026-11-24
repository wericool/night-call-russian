"""Extract the pinned editorial sources from a clean Night Call 1.3.7 copy."""

import argparse
import hashlib
import json
import shutil
from collections import defaultdict
from pathlib import Path
from tempfile import TemporaryDirectory

from corpus import ROOT, read_json


def corrected_node(node):
    from UnityPy.helpers.TypeTreeNode import TypeTreeNode

    flat = node.to_dict_list()
    for i, field in enumerate(flat):
        if field['m_Name'] == 'm_Enabled':
            field['m_MetaFlag'] |= 16384
        if (field['m_Type'] == 'string' and i + 3 < len(flat)
                and flat[i + 1]['m_Type'] == 'Array' and flat[i + 3]['m_Type'] != 'char'):
            field['m_Type'] = 'vector'
        field.pop('m_Children', None)
    return TypeTreeNode.from_list(flat)


def check_game(game_data, manifest):
    for name, expected in manifest['files'].items():
        path = game_data / name
        if not path.is_file() or hashlib.sha256(path.read_bytes()).hexdigest() != expected:
            raise SystemExit(f'Нужен исходный файл Night Call 1.3.7: {name}')


def extract(game_data, manifest, temporary):
    import UnityPy
    from UnityPy.helpers.TypeTreeGenerator import TypeTreeGenerator

    generator = TypeTreeGenerator('2018.4.0f1')
    generator.load_local_dll_folder(str(game_data / 'Managed'))
    objects = defaultdict(list)
    for row in manifest['objects']:
        objects[row['asset']].append(row)
    for asset, rows in objects.items():
        env = UnityPy.load(str(game_data / asset))
        env.typetree_generator = generator
        if asset != 'globalgamemanagers.assets':
            env.load_file(str(game_data / 'globalgamemanagers.assets'))
        serialized = next(file for file in env.assets if file.name == asset)
        for row in rows:
            obj = serialized.objects[row['path_id']]
            if row['cls'] == 'TextAsset':
                text = obj.read()
                script = text.m_Script
                if isinstance(script, bytes):
                    script = script.decode('utf-8')
                data = {'m_Name': text.m_Name, 'm_Script': script}
            else:
                data = obj.read_typetree(nodes=corrected_node(obj.generate_monobehaviour_node()))
            if data['m_Name'] != row['name']:
                raise ValueError(f'Unexpected object: {asset}/{row["path_id"]}')
            output = temporary / row['file']
            output.parent.mkdir(parents=True, exist_ok=True)
            # The existing source-bound editorial records pin these JSON bytes.
            output.write_text(json.dumps(data, ensure_ascii=False, indent=2),
                              encoding='utf-8', newline='\r\n')
    (temporary / 'original/inventory.json').write_text(
        json.dumps(manifest['objects'], ensure_ascii=False, indent=2), encoding='utf-8')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('game_data', type=Path, help='папка Night Call_Data чистой копии игры')
    args = parser.parse_args()
    manifest = read_json(ROOT / 'source_manifest.json')
    game = args.game_data.resolve()
    check_game(game, manifest)
    with TemporaryDirectory(prefix='prepare-', dir=ROOT) as directory:
        work = Path(directory)
        extract(game, manifest, work)
        shutil.copytree(work / 'original', ROOT / 'original', dirs_exist_ok=True)
    for name in ['Assembly-CSharp.dll', 'Prompter.dll', 'PrompterCompiler.dll', 'UnityEngine.dll',
                 'UnityEngine.CoreModule.dll']:
        shutil.copyfile(game / 'Managed' / name, ROOT / 'review' / name)
    targets = ROOT / 'revision/data/Russian_Texts'
    targets.mkdir(parents=True, exist_ok=True)
    for source in (ROOT / 'upstream/data/Russian_Texts').glob('*.txt'):
        target = targets / source.name
        if not target.exists():
            shutil.copyfile(source, target)
    print(f'Извлечено объектов: {len(manifest["objects"])}.')


if __name__ == '__main__':
    main()
