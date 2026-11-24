"""Restore one original French menu order by moving complete choice objects."""
import hashlib,json
from corpus import ROOT,read_json,command,emote
NAME='045_vero_01';PASSAGE='vero-01-story2-message';FIELD='french_choice_order_restorations';ORDER=[0,2,1]
EN_SHA='191d6c3c139070c4b7a26f6249ea7eaa43a6c5e3f3504409210a010cc8697f01'
FR_SHA='90b1449d62818a2bf2780b316ea56233455ab5c739561e5c28b8c9b01e07bd26'

def record():
 return dict(passage=PASSAGE,source_indices_in_french_order=list(ORDER),english_sha256=EN_SHA,french_sha256=FR_SHA)

def packet(name,review):
 fixes=(review or {}).get(FIELD,[]);assert isinstance(fixes,list)
 if name!=NAME:assert not fixes,(name,'No accepted French order restoration for this scene');return None
 assert len(fixes)==1 and review.get('structural_alignment') and review.get('source_sha256')==FR_SHA
 fix=fixes[0];expected=record();assert set(fix)==set(expected)|{'reason'} and all(fix[k]==v for k,v in expected.items())
 assert type(fix['source_indices_in_french_order']) is list and all(type(i) is int for i in fix['source_indices_in_french_order'])
 assert isinstance(fix['reason'],str) and fix['reason'].strip();return fix

def apply_order(source,reviewed,passage):
 fix=packet(source['name'],reviewed.get('french_review',{}))
 if fix is not None and passage['_title']==PASSAGE:
  assert len(passage['_choices'])==3
  passage['_choices']=[passage['_choices'][i] for i in ORDER]
 return passage

def validate(name,review,english,french):
 if packet(name,review) is None:return
 assert english['kind']==french['kind']=='compiled' and english['original_file']==french['original_file']
 raw=read_json(ROOT/english['original_file']);assert raw['m_Name']==NAME;originals={r['lang']:r['dialog'] for r in raw['dialogs']}
 for lang,src,pin in [(1,english,EN_SHA),(0,french,FR_SHA)]:
  digest=hashlib.sha256(json.dumps(originals[lang],ensure_ascii=False,sort_keys=True).encode()).hexdigest();assert digest==src['source_sha256']==pin and src['passages']==originals[lang]['_passages']
 assert originals[0]['_variables']==originals[1]['_variables'] and originals[0]['_user_datas'][1:]==originals[1]['_user_datas'][1:]
 ep={p['_title']:p for p in english['passages']};fp={p['_title']:p for p in french['passages']};assert ep.keys()==fp.keys() and len(ep)==26
 skeleton=lambda lines:[s if command(s) else '<TEXT>' for s in lines]
 for title,p in ep.items():
  f=fp[title];assert skeleton(p['_lines'])==skeleton(f['_lines'])
  assert {k:v for k,v in p.items() if k not in ['_lines','_choices']}=={k:v for k,v in f.items() if k not in ['_lines','_choices']}
  choices=[p['_choices'][i] for i in ORDER] if title==PASSAGE else p['_choices'];assert len(choices)==len(f['_choices'])
  for a,b in zip(choices,f['_choices']):assert {k:v for k,v in a.items() if k!='_text'}=={k:v for k,v in b.items() if k!='_text'} and emote(a['_text'])==emote(b['_text'])
 assert [c['_link'] for c in fp[PASSAGE]['_choices']]==['vero-01-story2-message-emoji','vero-01-story2-message-after','vero-01-story2-message-sorry']
 assert [emote(c['_text']) for c in fp[PASSAGE]['_choices']]==['','',':smile:']
