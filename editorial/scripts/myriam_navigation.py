"""One source-pinned French default link; no command or general graph override."""
from copy import deepcopy
from functools import lru_cache
import hashlib,json
from corpus import ROOT,read_json,command,emote

CASE=dict(name='073_myriam_01',original_file='original/DialogObjectScript/sharedassets0.assets/2272.json',
 english_sha256='3517065b7bd30ba80d03a6f627c32b0f4b6e668b5f66067639ddd25e538373e3',
 french_sha256='df62c4e3f2e9f2c0e8245bb92510732571b606602e526a1c703577776a6d2ce7',
 passage='myriam-01-story01-introA',english_target='myriam-01-story01',french_target='myriam-01-story01-introB')

@lru_cache(maxsize=4)
def _originals(path,mtime,size):
    raw=read_json(ROOT/path);src={d['lang']:d['dialog'] for d in raw['dialogs']}
    assert raw['m_Name']==CASE['name']
    result={}
    for lang,key in [(1,'english_sha256'),(0,'french_sha256')]:
        digest=hashlib.sha256(json.dumps(src[lang],ensure_ascii=False,sort_keys=True).encode('utf8')).hexdigest()
        assert digest==CASE[key]
        result[lang]=dict(name=raw['m_Name'],kind='compiled',original_file=path,source_sha256=digest,passages=src[lang]['_passages'])
    assert src[1]['_variables']==src[0]['_variables']==[dict(_name='tell',_value=0)]
    return result

def originals():
    p=ROOT/CASE['original_file'];s=p.stat()
    return _originals(CASE['original_file'],s.st_mtime_ns,s.st_size)

def validate_default_links(name,review,english,french):
    fixes=review.get('default_link_restorations',[]);assert isinstance(fixes,list)
    if name!=CASE['name']:
        assert not fixes,(name,'No approved original-French default-link restoration')
        return
    assert len(fixes)==1,'The original French swallow path must be explicitly retained'
    assert review.get('structural_alignment') and review['source_sha256']==CASE['french_sha256']
    fix=fixes[0];assert set(fix)=={'passage','english_target','french_target','reason'}
    assert all(type(fix[k]) is str and fix[k].strip() for k in fix)
    assert all(fix[k]==CASE[k] for k in ['passage','english_target','french_target'])
    src=originals()
    for given,lang,key in [(english,1,'english_sha256'),(french,0,'french_sha256')]:
        assert given['name']==name and given['kind']=='compiled' and given['original_file']==CASE['original_file']
        assert given['source_sha256']==CASE[key] and given['passages']==src[lang]['passages'],'Unpinned or forged source'
    en={p['_title']:p for p in english['passages']};fr={p['_title']:p for p in french['passages']}
    assert len(en)==len(fr)==38 and set(en)==set(fr)
    assert en[fix['passage']]['_link']==fix['english_target'] and fr[fix['passage']]['_link']==fix['french_target']
    for lang,data,line in [(1,en,'She swallows.'),(0,fr,'Elle déglutit.')]:
        p=data[fix['french_target']]
        assert p['_lines']==[line] and not p['_choices'] and not p['_variable_modifiers'] and p['_link']==fix['english_target']
    # Exactly the known three RAW source differences remain visible here. This
    # validator owns only the link. The two command slots are independently
    # restored/validated by myriam_french_commands from 26.10.2026 onward.
    command_differences={
      'myriam-01-start':(['$$ music-control: 0 $$','$$ reveal: 073_02 $$','$$ anim: IDLE_DRIVER $$','$$ music-control: 20 $$'],
                           ['$$ travel: start $$','$$ music-control: 0 $$','$$ reveal: 073_02 $$','$$ anim: IDLE_DRIVER $$','$$ music-control: 20 $$']),
      'myriam-01-changeofheart':(['$$ movie $$','$$ travel: stop $$','$$ travel: stop $$'],['$$ movie $$','$$ movie: stop $$','$$ travel: stop $$'])}
    for title,a in en.items():
        b=fr[title];aa={k:v for k,v in a.items() if k not in ['_lines','_choices']};bb={k:v for k,v in b.items() if k not in ['_lines','_choices']}
        if title==fix['passage']:aa['_link']=fix['french_target']
        assert aa==bb
        x=[s for s in a['_lines'] if command(s)];y=[s for s in b['_lines'] if command(s)]
        if title in command_differences:assert (x,y)==command_differences[title]
        else:assert x==y
        assert sum(not command(s) for s in a['_lines'])==sum(not command(s) for s in b['_lines'])
        assert [{k:v for k,v in c.items() if k!='_text'} for c in a['_choices']]==[{k:v for k,v in c.items() if k!='_text'} for c in b['_choices']]
        assert [emote(c['_text']) for c in a['_choices']]==[emote(c['_text']) for c in b['_choices']]
    for field in ['additional_french_lines','additional_french_choices','condition_flag_overrides','choice_target_restorations','source_link_defect_repairs']:
        assert not review.get(field),('No second structural repair in this source-pinned exception',field)

def apply_default_links(source,reviewed,passage):
    fixes=reviewed.get('french_review',{}).get('default_link_restorations',[])
    if source['name']!=CASE['name'] and not fixes:return passage
    validate_default_links(source['name'],reviewed['french_review'],source,originals()[0])
    if passage['_title']!=CASE['passage']:return passage
    result=deepcopy(passage);assert result['_link']==CASE['english_target']
    result['_link']=CASE['french_target'];return result
