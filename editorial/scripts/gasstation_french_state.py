"""One original-French TextAsset state write; retain case-sensitive names."""
import hashlib
from corpus import ROOT, read_json
from sleep_french_navigation import skeleton

NAME = '965_gasstation_01_intro_eng'
PASSAGE = 'gasstation-01-intro-hub'
FIELD = 'french_gasstation_state_restorations'
EN_SHA = 'ef6bb2392ad6be7f1e26fdd8925bc7afa8ef1e9f7535f7d6c27bd80d4f78b3d0'
FR_SHA = '13d3131909399ca7bdaf3709971756d1b93f8b67fe4645e71f54b49f1e030b71'
WRITE = '{ _Gasstation.Visit = 1 }'

def insert_write(content):
    lines = content.splitlines(keepends=True)
    matches = [i for i, line in enumerate(lines) if line.strip() == '=== ' + PASSAGE]
    assert len(matches) == 1, 'Exactly one original station hub required'
    index = matches[0]
    tail = []
    for line in lines[index + 1:]:
        if line.strip().startswith('==='): break
        if line.strip() and not line.strip().startswith('%%'): tail.append(line.strip())
    assert tail == ['-> ??gasstation-01-introA | gasstation-01-introB | gasstation-01-introC | gasstation-01-introD | gasstation-01-introE']
    ending = '\r\n' if lines[index].endswith('\r\n') else '\n'
    lines.insert(index + 1, '\t\t' + WRITE + ending)
    return ''.join(lines)

def validate(name, review, english, french):
    fixes = review.get(FIELD, [])
    assert isinstance(fixes, list)
    if name != NAME:
        assert not fixes, 'Station state exception cannot apply to another scene'
        return
    assert len(fixes) == 1 and english['kind'] == french['kind'] == 'script'
    fix = fixes[0]
    assert set(fix) == {'passage', 'write', 'english_sha256', 'french_sha256', 'reason'}
    assert fix['passage'] == PASSAGE and fix['write'] == WRITE and fix['reason'].strip()
    en = read_json(ROOT / english['original_file'])['m_Script']
    fr = read_json(ROOT / french['original_file'])['m_Script']
    assert english['script'] == en
    assert hashlib.sha256(en.encode('utf8')).hexdigest() == english['source_sha256'] == fix['english_sha256'] == EN_SHA
    assert hashlib.sha256(fr.encode('utf8')).hexdigest() == french['source_sha256'] == fix['french_sha256'] == FR_SHA
    assert skeleton(insert_write(en)) == skeleton(fr), 'No other script controls may change'

def apply_repair(source, reviewed, content):
    if source['name'] != NAME: return content
    meta = reviewed['french_review']
    validate(NAME, meta, source, dict(kind='script', source_sha256=meta['source_sha256'], original_file=meta['original_file']))
    return insert_write(content)
