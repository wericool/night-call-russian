"""Two exact FR command slots in Myriam01; keep the prior swallow-link repair."""
import copy
from corpus import command,emote
from myriam_navigation import CASE as SOURCE_CASE,originals

FIELD='french_myriam_command_restorations'
CASE=dict(name='073_myriam_01',original_file=SOURCE_CASE['original_file'],
    english_sha256=SOURCE_CASE['english_sha256'],french_sha256=SOURCE_CASE['french_sha256'],
    edits=[dict(passage='myriam-01-start',index=0,operation='insert',
        before='',after='$$ travel: start $$',next_line='MYRIAM : « I think they hit their son. »'),
        dict(passage='myriam-01-changeofheart',index=9,operation='replace',
        before='$$ travel: stop $$',after='$$ movie: stop $$',
        next_line='She stares at the streetlamps and the occasional pedestrian.')])

def packet(name,review):
    row=(review or {}).get(FIELD)
    if name!=CASE['name']:
        assert row is None,(name,'No accepted Myriam command restoration')
        return None
    assert isinstance(row,dict) and set(row)==set(CASE)|{'reason'}
    assert all(row[k]==v for k,v in CASE.items()),'Exact original-source decision required'
    assert isinstance(row['reason'],str) and row['reason'].strip()
    for edit in row['edits']:assert type(edit['index']) is int
    return row

def source_lines(name,review,passage,lines):
    row=packet(name,review);result=list(lines)
    if row is None:return result
    for edit in row['edits']:
        if passage['_title']!=edit['passage']:continue
        i=edit['index']
        if edit['operation']=='insert':
            assert result[i]==edit['next_line'],'Original opening anchor changed'
            result.insert(i,edit['after'])
        else:
            assert edit['operation']=='replace' and result[i]==edit['before']
            assert result[i+1]==edit['next_line'],'Original cutscene-stop boundary changed'
            result[i]=edit['after']
    return result

def validate(name,review,english,french):
    row=packet(name,review)
    if row is None:return
    assert review.get('structural_alignment') and review['source_sha256']==CASE['french_sha256']
    raw=originals()
    for src,lang,key in [(english,1,'english_sha256'),(french,0,'french_sha256')]:
        assert src['name']==name and src['kind']=='compiled' and src['original_file']==CASE['original_file']
        assert src['source_sha256']==CASE[key] and src['passages']==raw[lang]['passages'],'Forged source with old digest'
    links=review.get('default_link_restorations',[])
    assert len(links)==1
    assert all(links[0][k]==SOURCE_CASE[k] for k in ['passage','english_target','french_target'])
    ep={p['_title']:p for p in english['passages']};fp={p['_title']:p for p in french['passages']}
    assert len(ep)==len(fp)==38 and ep.keys()==fp.keys()
    skeleton=lambda lines:[s if command(s) else '<TEXT>' for s in lines]
    for title,p in ep.items():
        q=fp[title];view=copy.deepcopy(p)
        if title==links[0]['passage']:
            assert view['_link']==links[0]['english_target']
            view['_link']=links[0]['french_target']
        assert {k:v for k,v in view.items() if k not in ['_lines','_choices']}=={k:v for k,v in q.items() if k not in ['_lines','_choices']}
        assert skeleton(source_lines(name,review,p,p['_lines']))==skeleton(q['_lines']),(name,title,'Unreviewed event/text slot difference')
        assert [{k:v for k,v in c.items() if k!='_text'} for c in p['_choices']]==[{k:v for k,v in c.items() if k!='_text'} for c in q['_choices']]
        assert [emote(c['_text']) for c in p['_choices']]==[emote(c['_text']) for c in q['_choices']]
    for edit in row['edits']:
        q=fp[edit['passage']]['_lines'];i=edit['index']
        assert q[i]==edit['after']
    for field in ['additional_french_lines','additional_french_choices','condition_flag_overrides','choice_target_restorations','source_link_defect_repairs']:
        assert not review.get(field),('No unreviewed structural bridge',field)
