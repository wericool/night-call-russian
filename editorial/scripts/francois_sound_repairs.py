"""Two proven !sfx typos shared by the original French and English scripts.

The original SFX handler accepts action 'sfx' and the parameter '!event'.
It does not recognize action '!sfx'. Only these two source-pinned slots change.
"""
import hashlib, json
from corpus import ROOT, read_json, command

NAME = '018_francois_04'
FIELD = 'francois_sound_repairs'
EN_SHA = '51bccb47c2bbebceef0d3cf77b2a36f1222c1e9beee8ebda06466ff3f6e32bce'
FR_SHA = '4d5e42ef3fd919fc19ba766184a00da5bc050f6b9a3fc7844ac6c8d7b1cdf98c'
OLD = '$$ !sfx: francois_paper_scratch $$'
FR_OLD = '$$\u00a0!sfx: francois_paper_scratch $$'
NEW = '$$ sfx: !francois_paper_scratch $$'
SLOTS = [('francois-04-notes01', 10), ('francois-04-notes02', 7)]


def packet(name, review):
    fixes = (review or {}).get(FIELD, [])
    assert isinstance(fixes, list)
    if name != NAME:
        assert not fixes, (name, 'No accepted shared-source SFX repair')
        return []
    assert len(fixes) == 2, 'Both proven original sound typos must be repaired explicitly'
    for fix, (title, index) in zip(fixes, SLOTS):
        assert set(fix) == {'passage','line_index','english_command','french_command','corrected_command','reason'}
        assert type(fix['line_index']) is int
        assert (fix['passage'],fix['line_index'],fix['english_command'],fix['french_command'],fix['corrected_command']) == (title,index,OLD,FR_OLD,NEW)
        assert isinstance(fix['reason'],str) and fix['reason'].strip()
    return fixes


def source_lines(name, review, passage, lines):
    result = list(lines)
    for fix in packet(name, review):
        if passage['_title'] == fix['passage']:
            i = fix['line_index']; assert result[i] == OLD
            result[i] = NEW
    return result


def validate(name, review, english, french):
    fixes = packet(name, review)
    if not fixes:
        return
    assert review.get('structural_alignment')
    assert english['kind'] == french['kind'] == 'compiled'
    assert english['original_file'] == french['original_file']
    raw = read_json(ROOT/english['original_file']); assert raw['m_Name'] == NAME
    sources = {r['lang']:r['dialog'] for r in raw['dialogs']}
    for lang, src, pin in [(1,english,EN_SHA),(0,french,FR_SHA)]:
        digest = hashlib.sha256(json.dumps(sources[lang],ensure_ascii=False,sort_keys=True).encode('utf8')).hexdigest()
        assert digest == src['source_sha256'] == pin
        assert src['passages'] == sources[lang]['_passages']
    assert sources[0]['_variables'] == sources[1]['_variables']
    ep={p['_title']:p for p in english['passages']};fp={p['_title']:p for p in french['passages']}
    assert ep.keys() == fp.keys()
    for fix in fixes:
        assert fp[fix['passage']]['_lines'][fix['line_index']] == FR_OLD
    for title,p in ep.items():
        q=fp[title];target=list(q['_lines'])
        for fix in fixes:
            if title == fix['passage']:target[fix['line_index']]=NEW
        skeleton=lambda lines:[s.replace('\u00a0',' ') if command(s) else '<TEXT>' for s in lines]
        assert skeleton(source_lines(name,review,p,p['_lines'])) == skeleton(target)
        assert {k:v for k,v in p.items() if k not in ['_lines','_choices']} == {k:v for k,v in q.items() if k not in ['_lines','_choices']}
        assert [{k:v for k,v in c.items() if k!='_text'} for c in p['_choices']] == [{k:v for k,v in c.items() if k!='_text'} for c in q['_choices']]
    # The same sound starts, pauses, restarts and pauses again in these two blocks.
    assert ep['francois-04-notes01']['_lines'][2] == ep['francois-04-notes01']['_lines'][14] == '$$ sfx: francois_paper_scratch $$'
    assert ep['francois-04-notes01']['_link'] == 'francois-04-notes02'
