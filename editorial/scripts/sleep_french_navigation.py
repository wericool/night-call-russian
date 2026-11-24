"""One source-pinned French random target, aligned to the retained carrier ID."""
import hashlib,re,copy
from corpus import ROOT,read_json,parse_text_content,command
NAME='930_sleep_eng';PASSAGE='sleep-tired-mid-hub'
OLD='?? sleep-tired-mid01 | sleep-tired-mid02'
NEW='?? sleep-tired-mid01 | sleep-tired-high01'
FR_SHA='c3d075947ae93d72441306295ec42a45fc63501f87511824ad9c76596c159c02'
EN_SHA='b4b09448d009aff29f42084c8fbac2ea3cde4921d60a338d7be902964dc5ce6a'
FIELD='french_random_target_restorations'

def skeleton(raw):
    blocks,duplicates=parse_text_content(raw);assert not duplicates
    visible={i for p in blocks.values() for i,t in zip(p['line_numbers'],p['lines']) if not command(t)}
    choices={c['number']:c['link'] for p in blocks.values() for c in p['choices']}
    return ['TEXT' if i in visible else '* -> '+choices[i] if i in choices else line.strip() for i,line in enumerate(raw.splitlines(),1) if line.strip() and not line.strip().startswith('%%')]

def replace_link(raw,old=OLD,new=NEW):
    lines=raw.splitlines(keepends=True);title=None;count=0
    for i,line in enumerate(lines):
        value=line.strip()
        if value.startswith('==='):title=value[3:].strip()
        elif title==PASSAGE and value=='-> '+old:
            assert line.count(old)==1;lines[i]=line.replace(old,new,1);count+=1
    assert count==1,'Expected exactly one pinned random-link line'
    return ''.join(lines)

def validate(name,review,english,french):
    fixes=review.get(FIELD,[]);assert isinstance(fixes,list)
    if name!=NAME:assert not fixes;return
    assert english['kind']==french['kind']=='script' and len(fixes)==1
    fix=fixes[0]
    assert set(fix)=={'passage','source_link','french_link','target_link','french_target','carrier_target','french_source_sha256','english_source_sha256','reason'}
    assert fix['passage']==PASSAGE and fix['source_link']==fix['french_link']==OLD and fix['target_link']==NEW
    assert fix['french_target']=='sleep-tired-mid02' and fix['carrier_target']=='sleep-tired-high01' and fix['reason']
    assert fix['french_source_sha256']==french['source_sha256']==FR_SHA
    assert fix['english_source_sha256']==english['source_sha256']==EN_SHA
    en=english['script'];fr=read_json(ROOT/french['original_file'])['m_Script']
    assert hashlib.sha256(en.encode('utf8')).hexdigest()==EN_SHA
    assert hashlib.sha256(fr.encode('utf8')).hexdigest()==FR_SHA
    eb,_=parse_text_content(en);fb,_=parse_text_content(fr)
    assert 'sleep-tired-mid02' not in eb and 'sleep-tired-high01' in eb
    assert 'sleep-tired-mid02' in fb and 'sleep-tired-high01' not in fb
    assert len(eb)==len(fb)==82
    assert len(eb['sleep-tired-high01']['lines'])==len(fb['sleep-tired-mid02']['lines'])==3
    # Map only the already documented internal ID; all source technical fields match.
    aligned=re.sub(r'(?<![\w-])sleep-tired-mid02(?![\w-])','sleep-tired-high01',fr)
    assert skeleton(replace_link(en))==skeleton(aligned)
    for raw in [en,fr]:
        selected=[];title=None
        for line in raw.splitlines():
            value=line.strip()
            if value.startswith('==='):title=value[3:].strip()
            elif title==PASSAGE and value and not value.startswith('%%'):selected.append(value)
        assert selected==['-> '+OLD]

def apply_repair(source,reviewed,content):
    if source['name']!=NAME:return content
    meta=reviewed['french_review']
    fr={'kind':'script','source_sha256':meta['source_sha256'],'original_file':meta['original_file']}
    validate(NAME,meta,source,fr)
    return replace_link(content)
