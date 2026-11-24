"""One exact French door-SFX relocation; no new sound or gameplay transition."""
from corpus import ROOT, read_json, command, emote
import hashlib,json

NAME='057_annabelle_01'
FIELD='french_door_sfx_restorations'
CASE=dict(english_sha256='0560474569edd0792134561e86ab36e0ee7a0e0928ae916a4d000fb347ea4207',
          french_sha256='3c578cde15199afa8803373958cf72f268c1542660fb722dbf16841295ea0152',
          from_passage='annabelle-01-gasstation3',from_english_line_index=7,
          to_passage='annabelle-01-gasstation2-outro',to_english_line_index=4,
          to_french_line_index=4,command='$$ sfx: sfx_gameplay_car_openclose_from_int $$')


def packet(name,review):
    rows=(review or {}).get(FIELD,[])
    assert isinstance(rows,list)
    if name!=NAME:
        assert not rows,(name,'No accepted French door relocation for this scene')
        return None
    assert len(rows)==1,'One exact French door relocation is mandatory'
    row=rows[0];assert set(row)==set(CASE)|{'reason'}
    assert all(row[k]==v for k,v in CASE.items())
    for k in ['from_english_line_index','to_english_line_index','to_french_line_index']:
        assert type(row[k]) is int
    assert isinstance(row['reason'],str) and row['reason'].strip()
    return row


def source_lines(name,review,passage,lines):
    fix=packet(name,review);result=list(lines)
    if fix is None:return result
    if passage['_title']==fix['from_passage']:
        i=fix['from_english_line_index'];assert result[i]==fix['command']
        assert result[i-1]=='$$ reveal: 057_04 $$';result.pop(i)
    elif passage['_title']==fix['to_passage']:
        i=fix['to_english_line_index'];assert result[i-1]=='$$ movie: black $$'
        assert result[i]=='She gets out of the cab and puts her things on a picnic table.'
        result.insert(i,fix['command'])
    return result


def validate(name,review,english,french):
    fix=packet(name,review)
    if fix is None:return
    assert review.get('structural_alignment')
    assert english['kind']==french['kind']=='compiled'
    assert english['original_file']==french['original_file']
    raw=read_json(ROOT/english['original_file']);assert raw['m_Name']==name
    originals={r['lang']:r['dialog'] for r in raw['dialogs']}
    for lang,source,key in [(1,english,'english_sha256'),(0,french,'french_sha256')]:
        h=hashlib.sha256(json.dumps(originals[lang],ensure_ascii=False,sort_keys=True).encode()).hexdigest()
        assert h==source['source_sha256']==fix[key]
        assert source['passages']==originals[lang]['_passages']
    assert originals[0]['_variables']==originals[1]['_variables']
    ep={p['_title']:p for p in english['passages']};fp={p['_title']:p for p in french['passages']}
    assert ep.keys()==fp.keys() and len(ep)==87
    skeleton=lambda lines:[s if command(s) else '<TEXT>' for s in lines]
    for title,p in ep.items():
        q=fp[title]
        assert {k:v for k,v in p.items() if k not in ['_lines','_choices']}=={k:v for k,v in q.items() if k not in ['_lines','_choices']}
        assert skeleton(source_lines(name,review,p,p['_lines']))==skeleton(q['_lines'])
        assert len(p['_choices'])==len(q['_choices'])
        for a,b in zip(p['_choices'],q['_choices']):
            assert {k:v for k,v in a.items() if k!='_text'}=={k:v for k,v in b.items() if k!='_text'}
            assert emote(a['_text'])==emote(b['_text'])
    target=fp[fix['to_passage']]['_lines'];assert target[fix['to_french_line_index']]==fix['command']
    assert fix['command'] not in fp[fix['from_passage']]['_lines']
    assert ep[fix['to_passage']]['_link']==fix['from_passage']
