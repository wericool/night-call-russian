"""Two source-pinned scenes: presentation order, Mathieu's FR tip and ID alias.

The ID mapping is a validation view only. It never renames live carrier IDs.
"""
import copy,hashlib,json
from corpus import ROOT,read_json,command,emote

FIELD='french_tps_mathieu_restorations'
CASES={
 '066_tps_01':dict(name='066_tps_01',english_sha256='b34f4f707d6c3252cce374414a3526a1756c7096c7918bf70b78acb2f85b1d72',french_sha256='16826779b9c9417b9371d7f7dae2550cf01bae903d5014ef62acefc0c4073bfc',
  command_slots=[
   dict(passage='tps-01-game-xenofex-beg-help',index=0,before=['$$ anim-link: SH_BOARD_2_DRIVER $$','$$ anim-link: PI_BASIC_2_DRIVER $$'],after=['$$ anim-link: PI_BASIC_2_DRIVER $$','$$ anim-link: SH_BOARD_2_DRIVER $$']),
   dict(passage='tps-01-game-xenofex-fight-turn01x',index=5,before=['$$ anim-link: SH_BOARD_DICE_PIERRE $$','She rolls the die.'],after=['She rolls the die.','$$ anim-link: SH_BOARD_DICE_PIERRE $$']),
   dict(passage='tps-01-game-xenofex-fight-turn02p',index=3,before=['$$ movie $$','$$ anim: IDLE_SHAINE_DRIVER $$','$$ anim: IDLE_PIERRE_DRIVER $$','PIERRE: « I’m role-playing, Shaïne. Sorry. That’s just how it is. »'],after=['PIERRE: « I’m role-playing, Shaïne. Sorry. That’s just how it is. »','$$ movie $$','$$ anim: IDLE_SHAINE_DRIVER $$','$$ anim: IDLE_PIERRE_DRIVER $$'])],markers=[],id_alignment=[]),
 '067_mathieu_01':dict(name='067_mathieu_01',english_sha256='0b27f70c55f7f241c5444a9c7cee9c749b2d4585e6665ebfa246ba0c9440b2f6',french_sha256='03108d2c5efb6ebac65b7e8c24be8bcca69fdec3bf05938c076e65b379c808a4',
  command_slots=[dict(passage='mathieu-01-start',index=1,before=['$$ tip: medium $$'],after=['$$ tip: low $$']),dict(passage='mathieu-01-silence02',index=10,before=['$$ music-control: 60 $$','$$ anim-link: DRIVER_2_WINDOW $$'],after=['$$ anim-link: DRIVER_2_WINDOW $$','$$ music-control: 60 $$'])],
  markers=[dict(passage='mathieu-01-silence01',index=0,source_marker='',french_marker=':silence:')],
  id_alignment=[dict(carrier='mathieu-01-hospital-car accident',french='mathieu-01-hospital-caraccident')])}


def packet(name,review):
    row=(review or {}).get(FIELD)
    if name not in CASES:
        assert row is None,(name,'No accepted TPS/Mathieu technical decision')
        return None
    assert isinstance(row,dict) and set(row)==set(CASES[name])|{'reason'}
    assert all(row[k]==v for k,v in CASES[name].items())
    assert isinstance(row['reason'],str) and row['reason'].strip()
    for item in row['command_slots']+row['markers']:assert type(item['index']) is int
    return row


def source_lines(name,review,passage,lines):
    row=packet(name,review);result=list(lines)
    if row is None:return result
    for fix in row['command_slots']:
        if fix['passage']!=passage['_title']:continue
        i=fix['index'];assert result[i:i+len(fix['before'])]==fix['before']
        result[i:i+len(fix['before'])]=fix['after']
    return result


def expected_marker(review,title,index,marker):
    data=(review or {}).get(FIELD)
    if data is None:return marker
    row=packet(data['name'],review)
    for fix in row['markers']:
        if (title,index)==(fix['passage'],fix['index']):
            assert marker==fix['source_marker'];return fix['french_marker']
    return marker


def canonical_passage(passage,row):
    """Bijection for comparison, never called by render/export."""
    p=copy.deepcopy(passage);mapping={a['carrier']:a['french'] for a in row['id_alignment']}
    p['_title']=mapping.get(p['_title'],p['_title']);p['_link']=mapping.get(p['_link'],p['_link'])
    for c in p['_choices']:c['_link']=mapping.get(c['_link'],c['_link'])
    return p


def validate(name,review,english,french):
    row=packet(name,review)
    if row is None:return
    assert review.get('structural_alignment') and english['kind']==french['kind']=='compiled'
    assert english['original_file']==french['original_file']
    raw=read_json(ROOT/english['original_file']);assert raw['m_Name']==name;originals={x['lang']:x['dialog'] for x in raw['dialogs']}
    for lang,source,key in [(1,english,'english_sha256'),(0,french,'french_sha256')]:
        h=hashlib.sha256(json.dumps(originals[lang],ensure_ascii=False,sort_keys=True).encode()).hexdigest();assert h==source['source_sha256']==row[key]
        assert source['passages']==originals[lang]['_passages']
    assert originals[1]['_variables']==originals[0]['_variables']
    ep={canonical_passage(p,row)['_title']:p for p in english['passages']};fp={p['_title']:p for p in french['passages']};assert ep.keys()==fp.keys()
    assert len(ep)==(208 if name=='066_tps_01' else 29)
    skeleton=lambda lines:[s if command(s) else '<TEXT>' for s in lines]
    for title,p in ep.items():
        q=fp[title];view=canonical_passage(p,row)
        assert {k:v for k,v in view.items() if k not in ['_lines','_choices']}=={k:v for k,v in q.items() if k not in ['_lines','_choices']}
        assert skeleton(source_lines(name,review,p,p['_lines']))==skeleton(q['_lines'])
        assert len(p['_choices'])==len(q['_choices'])
        for i,(a,b) in enumerate(zip(view['_choices'],q['_choices'])):
            assert {k:v for k,v in a.items() if k!='_text'}=={k:v for k,v in b.items() if k!='_text'}
            assert expected_marker(review,p['_title'],i,emote(a['_text']))==emote(b['_text'])
    if row['id_alignment']:
        alias=row['id_alignment'][0];assert alias['carrier'] not in fp and alias['french'] not in {p['_title'] for p in english['passages']}
        assert sum(c['_link']==alias['carrier'] for p in english['passages'] for c in p['_choices'])==1
        assert sum(c['_link']==alias['french'] for p in french['passages'] for c in p['_choices'])==1
