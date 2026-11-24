"""Exact source-pinned French command alignments, with original slot positions."""
from corpus import ROOT, read_json, command
import hashlib, json

FIELD = 'french_command_restorations'
CASES = {
    '013_leia_01': dict(passage='leia-01-shock', english_line_index=5,
        french_line_index=5, source_command='$$ music-control: 20 $$',
        french_command='', operation='remove',
        english_sha256='1f0c73ddef871193dcbc28afe5330756ac7bed45e8be594d12cd80199d953b5c',
        french_sha256='857fe9da423012307abaac64979be1c2461fcfb86c69376f9f5ec0dcdc595e24'),
    '014_fragonard_08': dict(passage='frag-08-gameover', english_line_index=9,
        french_line_index=9, source_command='',
        french_command='$$ music-control: 80 $$', operation='insert',
        english_sha256='cd2a73f47c2b12ce2360f5c7efc272d9cf808b4d7c05f9c32e1c6d2acce91315',
        french_sha256='08ce03aa001ee8a87ec7dc076c1cdd85795f8fc5b5aa986e8a45048a7db2c264'),
    '029_jerome_02': dict(passage='jerome-02-return', english_line_index=2,
        french_line_index=2, source_command='$$ destination: 1 $$',
        french_command='$$ destination: 0 $$', operation='replace',
        english_sha256='6820ca7d2d94953f0c6a3d30565f24c7a37e847ecfc7fa8af46ef1c58b28c88d',
        french_sha256='a5a2802a9b7214190debbaa6ca71d84b05b690ab3da06a4de1a6101f8adc2cbf'),
}


def packet(name, review):
    fixes = (review or {}).get(FIELD, [])
    assert isinstance(fixes, list)
    if name not in CASES:
        assert not fixes, (name, 'No accepted command alignment for this scene')
        return None
    assert len(fixes) == 1, (name, 'One exact French command decision is mandatory')
    fix = fixes[0]; case = CASES[name]
    assert set(fix) == set(case) | {'reason'}
    assert all(fix[k] == v for k, v in case.items())
    assert type(fix['english_line_index']) is int and type(fix['french_line_index']) is int
    assert isinstance(fix['reason'], str) and fix['reason'].strip()
    return fix


def source_lines(name, review, passage):
    """Return only an explicitly aligned command list; never edit prose or inputs."""
    fix = packet(name, review)
    lines = list(passage['_lines'])
    from francois_sound_repairs import source_lines as repair_shared_sfx
    lines = repair_shared_sfx(name,review,passage,lines)
    from skaterz_french_structure import source_lines as restore_skaterz_animation
    lines = restore_skaterz_animation(name,review,passage,lines)
    from annabelle_french_door_sfx import source_lines as restore_annabelle_door
    lines = restore_annabelle_door(name,review,passage,lines)
    from tps_mathieu_french_technical import source_lines as restore_tps_mathieu
    lines = restore_tps_mathieu(name,review,passage,lines)
    from mathieu_followup_markers import source_lines as restore_mathieu_followup
    lines = restore_mathieu_followup(name,review,passage,lines)
    from myriam_french_commands import source_lines as restore_myriam_commands
    lines = restore_myriam_commands(name,review,passage,lines)
    from french_choice_markers import source_lines as restore_marker_slots
    lines = restore_marker_slots(name,review,passage,lines)
    from esmeralda_parent_alignment import source_lines as restore_esmeralda_parent_timing
    lines = restore_esmeralda_parent_timing(name,review,passage,lines)
    from chiara_radio_cutscene_repair import source_lines as repair_chiara_radio
    lines = repair_chiara_radio(name,review,passage,lines)
    from vero_travel_command_repair import source_lines as repair_vero_travel
    lines = repair_vero_travel(name,review,passage,lines)
    if fix is None or passage['_title'] != fix['passage']:
        return lines
    i = fix['english_line_index']
    if fix['operation'] == 'remove':
        assert lines[i] == fix['source_command']
        lines.pop(i)
    elif fix['operation'] == 'insert':
        assert lines[i] == '$$ anim-link: ANGERGUN_2_DRIVERGUN $$'
        lines.insert(i, fix['french_command'])
    else:
        assert fix['operation'] == 'replace'
        assert lines[i] == fix['source_command']
        lines[i] = fix['french_command']
    return lines


def validate(name, review, english, french):
    fix = packet(name, review)
    if fix is None:
        return
    case = CASES[name]
    assert review.get('structural_alignment')
    assert english['kind'] == french['kind'] == 'compiled'
    assert english['original_file'] == french['original_file']
    raw = read_json(ROOT/english['original_file'])
    assert raw['m_Name'] == name
    originals = {r['lang']: r['dialog'] for r in raw['dialogs']}
    for lang, src, key in [(1, english, 'english_sha256'), (0, french, 'french_sha256')]:
        digest = hashlib.sha256(json.dumps(originals[lang], ensure_ascii=False, sort_keys=True).encode('utf8')).hexdigest()
        assert digest == case[key] == src['source_sha256']
        assert src['passages'] == originals[lang]['_passages']
    ep = {p['_title']: p for p in english['passages']}
    fp = {p['_title']: p for p in french['passages']}
    assert ep.keys() == fp.keys()
    skeleton = lambda lines: [s if command(s) else '<TEXT>' for s in lines]
    for title, p in ep.items():
        q = fp[title]
        assert skeleton(source_lines(name, review, p)) == skeleton(q['_lines']), (name, title, 'Unreviewed command or slot difference')
        assert {k:v for k,v in p.items() if k not in ['_lines','_choices']} == {k:v for k,v in q.items() if k not in ['_lines','_choices']}
        assert [{k:v for k,v in c.items() if k!='_text'} for c in p['_choices']] == [{k:v for k,v in c.items() if k!='_text'} for c in q['_choices']]
    f = fp[fix['passage']]['_lines']
    if fix['operation'] == 'insert':
        assert f[fix['french_line_index']] == fix['french_command']
    elif fix['operation'] == 'remove':
        assert not command(f[fix['french_line_index']])
        assert fix['source_command'] not in f
    else:
        assert fix['operation'] == 'replace'
        assert f[fix['french_line_index']] == fix['french_command']
