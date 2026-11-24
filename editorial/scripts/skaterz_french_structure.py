"""Two exact original French fields, source-pinned and with full technical parity."""
import hashlib,json
from corpus import ROOT,read_json,command,emote
NAME='026_skaterz_01';FIELD='skaterz_french_restorations'
EN_SHA='39a8664c0fc474f124dd518d37a244f96acb72629285c0e96404afdee12a692d'
FR_SHA='8a82669248152fdc391be8b7043727a2f060c35974c93fad3ea350c2997f0f00'
LINK_PASSAGE='ultra-01-hotel-arrival'
OLD_LINK='yellow_found=2?ultra-01-hotel-found;;ultra-01-hotel-arrival2'
NEW_LINK='yellow_found>1?ultra-01-hotel-found;;ultra-01-hotel-arrival2'
ANIM_PASSAGE='ultra-01-start-mission-out'
OLD_ANIM='$$ anim-link: GREEN_PUSH_2_IDLE $$';NEW_ANIM='$$ anim: GREEN_IDLE $$'
CASES=[dict(kind='default_link',passage=LINK_PASSAGE,source=OLD_LINK,french=NEW_LINK),dict(kind='command',passage=ANIM_PASSAGE,line_index=5,source=OLD_ANIM,french=NEW_ANIM)]

def packet(name,review):
 fixes=(review or {}).get(FIELD,[]);assert isinstance(fixes,list)
 if name!=NAME:assert not fixes,(name,'No accepted Skaterz restoration for this scene');return []
 assert len(fixes)==2 and review.get('structural_alignment') and review.get('source_sha256')==FR_SHA
 for fix,case in zip(fixes,CASES):
  assert set(fix)==set(case)|{'reason'} and all(fix[k]==v for k,v in case.items())
  assert isinstance(fix['reason'],str) and fix['reason'].strip()
  if 'line_index' in fix:assert type(fix['line_index']) is int
 return fixes

def source_lines(name,review,passage,lines):
 result=list(lines)
 for fix in packet(name,review):
  if fix['kind']=='command' and passage['_title']==fix['passage']:
   i=fix['line_index'];assert result[i]==fix['source'];result[i]=fix['french']
 return result

def apply_link(name,review,title,link):
 for fix in packet(name,review):
  if fix['kind']=='default_link' and title==fix['passage']:assert link==fix['source'];return fix['french']
 return link

def validate(name,review,english,french):
 fixes=packet(name,review)
 if not fixes:return
 assert english['kind']==french['kind']=='compiled' and english['original_file']==french['original_file']
 raw=read_json(ROOT/english['original_file']);assert raw['m_Name']==NAME;sources={r['lang']:r['dialog'] for r in raw['dialogs']}
 for lang,src,pin in [(1,english,EN_SHA),(0,french,FR_SHA)]:
  digest=hashlib.sha256(json.dumps(sources[lang],ensure_ascii=False,sort_keys=True).encode('utf8')).hexdigest()
  assert digest==src['source_sha256']==pin and src['passages']==sources[lang]['_passages']
 assert sources[0]['_variables']==sources[1]['_variables']
 assert sources[0]['_user_datas'][1:]==sources[1]['_user_datas'][1:]
 ep={p['_title']:p for p in english['passages']};fp={p['_title']:p for p in french['passages']};assert ep.keys()==fp.keys() and len(ep)==90
 skeleton=lambda lines:[s if command(s) else '<TEXT>' for s in lines]
 for title,p in ep.items():
  q=fp[title];assert skeleton(source_lines(name,review,p,p['_lines']))==skeleton(q['_lines'])
  a={k:v for k,v in p.items() if k not in ['_lines','_choices']};a['_link']=apply_link(name,review,title,a['_link'])
  assert a=={k:v for k,v in q.items() if k not in ['_lines','_choices']}
  assert len(p['_choices'])==len(q['_choices'])
  for c,d in zip(p['_choices'],q['_choices']):assert {k:v for k,v in c.items() if k!='_text'}=={k:v for k,v in d.items() if k!='_text'} and emote(c['_text'])==emote(d['_text'])
 assert fp[LINK_PASSAGE]['_link']==NEW_LINK and fp[ANIM_PASSAGE]['_lines'][5]==NEW_ANIM
 assert all(t in ep for t in ['ultra-01-hotel-found','ultra-01-hotel-arrival2'])
 assert NEW_ANIM in fp['ultra-01-intro']['_lines']
 assert 'anim-link: GREEN_PUSH_2_IDLE -> GREEN_PUSHING_REV, GREEN_IDLE' in sources[0]['_user_datas']
