"""Read back the runtime export against current sources and editorial records.

This checks data integrity only. It does not establish literary quality or game UI
acceptance. An export's mere existence is not evidence that it is current.
"""
from full_review import *
from datetime import datetime


def same(a, b, where):
    assert a == b, ('Runtime export mismatch', where)


def main():
    sources = source_records()
    from french_sources import french_source_records,validate_french_review
    french_sources=french_source_records()
    reviews = {p.stem: read_json(p) for p in (REVIEWED).glob('*.json')}
    path = DEST/'reviewed_engine_input.json'
    exported = read_json(path)
    same(set(exported), {n for n in reviews if sources[n]['kind']=='compiled'}, 'scene coverage')
    calls = 0
    technical_calls = 0
    for name, dialog in exported.items():
        s, r = sources[name], reviews[name]
        if r.get('french_review') is not None:
            validate_french_review(name,r['french_review'],s,french_source=french_sources[name])
        same(r['source_sha256'], s['source_sha256'], name+' source hash')
        original = next(x['dialog'] for x in read_json(ROOT/s['original_file'])['dialogs'] if x['lang']==1)
        same(set(dialog), set(original), name+' dialog fields')
        for key in original:
            if key not in {'_passages','_user_datas'}:
                same(dialog[key], original[key], name+' '+key)
        same(len(dialog['_passages']), len(original['_passages']), name+' passage count')
        units = {u['passage']:u for u in r['passages']}
        same(set(units), {p['_title'] for p in original['_passages']}, name+' review coverage')
        for p, source in zip(dialog['_passages'], original['_passages']):
            where = name+' '+source['_title']
            unit = units[source['_title']]
            expected=apply_compiled_review(s,r,source)
            same(set(p), set(source), where+' passage fields')
            for key in source:
                if key not in {'_lines','_choices'}:
                    same(p[key], expected[key], where+' '+key)
            same(p['_lines'], expected['_lines'], where+' full editorial lines including guarded FR restorations')
            from french_command_alignment import source_lines as aligned_command_lines
            for i, line in enumerate(aligned_command_lines(name,r.get('french_review',{}),source)):
                if command(line):same(p['_lines'][i], line, where+' command position '+str(i))
                else:assert not command(p['_lines'][i]), (where,'text became a command',i)
            same([x for x in p['_lines'] if not command(x)], [x for x in expected['_lines'] if not command(x)], where+' editorial text')
            same(len(p['_choices']), len(expected['_choices']), where+' choice count')
            same([c['_text'] for c in p['_choices']], [c['_text'] for c in expected['_choices']], where+' editorial answers')
            for choice, before in zip(p['_choices'], expected['_choices']):
                same(set(choice), set(before), where+' choice fields')
                same(emote(choice['_text']), emote(before['_text']), where+' emote')
                for key in before:
                    if key!='_text':same(choice[key], before[key], where+' choice '+key)
        actual_calls = r.get('passenger_calls', [])
        expected_calls = passenger_calls(original['_user_datas'])
        same([c['index'] for c in actual_calls], [c['index'] for c in expected_calls], name+' call coverage')
        for target, source in zip(actual_calls, expected_calls):
            for key, value in source.items():same(target[key],value,name+' call source '+key)
            if target.get('technical_marker'):
                same(target['russian'],source['english'],name+' unchanged technical call marker')
                technical_calls += 1
        same(dialog['_user_datas'], apply_calls(original['_user_datas'],actual_calls), name+' call and animation metadata')
        calls += len(actual_calls)
    input_sha256 = hashlib.sha256(path.read_bytes()).hexdigest()
    runtime_path = DEST/'runtime_validation.json'
    runtime = read_json(runtime_path) if runtime_path.exists() else {}
    full_runtime_deferred = not (
        runtime.get('passed') and runtime.get('input_sha256') == input_sha256
        and all(hashlib.sha256((ROOT/source).read_bytes()).hexdigest() == expected
                for build in runtime['results'] for source, expected in build['sources'].items())
    )
    report = dict(passed=True, counts=dict(compiled_scenes=len(exported),translated_calls=calls-technical_calls,
        reviewed_call_positions=calls,technical_call_markers=technical_calls),
        input_sha256=input_sha256,
        full_runtime_deferred=full_runtime_deferred,
        validator='scripts/validate_editorial_input.py',
        validator_sha256=hashlib.sha256(Path(__file__).read_bytes()).hexdigest(),
        checked_at=datetime.now().astimezone().isoformat(),
        scope='Current reviewed texts, choices, source hashes, compiled structures and call metadata match exact runtime input.',
        boundaries=['Not a native asset rebuild, script compiler check or live gameplay verification.'])
    write_json(DEST/'editorial_validation.json',report)
    print('Editorial runtime input verified:',len(exported),'compiled scenes;',calls-technical_calls,'translated call previews;',technical_calls,'unchanged technical markers.')


if __name__=='__main__':main()
