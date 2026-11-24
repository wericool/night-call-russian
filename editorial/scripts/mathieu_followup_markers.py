"""Two FR markers and one relative animation slot; preserve prior FR02 repairs."""
import copy,hashlib,json
from corpus import ROOT,read_json,command,emote
FIELD='french_mathieu_followup_restorations'
CASES={
 '067_mathieu_02':dict(name='067_mathieu_02',english_sha256='daef858f225ab303ac3d6d6e2ad20a4482f55d2a85803ffaccf9dea4917a9772',french_sha256='6b4d30b2984bdedc50c60ee133ed3033253f689a197eb12b0ab269c0c70911cb',
  markers=[dict(passage='mathieu-02-story2',carrier_choice_index=2,french_choice_index=3,final_choice_index=3,source_marker=':silence:',french_marker='')],
  command_slots=[dict(passage='mathieu-02-father3',index=1,before=['“Meant it?”','$$ anim-link: THINK_2_DRIVER $$'],after=['$$ anim-link: THINK_2_DRIVER $$','“Meant it?”'])]),
 '067_mathieu_04':dict(name='067_mathieu_04',english_sha256='c933c54a8482d7f2b7319dd52b6a3b23d71fa1e4c9e5068314e07fec70e8f2fa',french_sha256='3cb36341f8e3b8dc6f3de6a981a6cbd0a53217de9593a5f42a2f0833b27b18cb',
  markers=[dict(passage='mathieu-04-mask2-arrival',carrier_choice_index=2,french_choice_index=2,final_choice_index=2,source_marker=':taxi:',french_marker='')],command_slots=[])}

def packet(name,review):
 row=(review or {}).get(FIELD)
 if name not in CASES:
  assert row is None,(name,'No accepted Mathieu followup decision');return None
 assert isinstance(row,dict) and set(row)==set(CASES[name])|{'reason'}
 assert all(row[k]==v for k,v in CASES[name].items())
 assert isinstance(row['reason'],str) and row['reason'].strip()
 for m in row['markers']:
  assert all(type(m[k]) is int for k in ['carrier_choice_index','french_choice_index','final_choice_index'])
 for c in row['command_slots']:assert type(c['index']) is int
 return row

def expected_marker(review,title,index,marker):
 data=(review or {}).get(FIELD)
 if data is None:return marker
 row=packet(data['name'],review)
 for fix in row['markers']:
  if (title,index)==(fix['passage'],fix['carrier_choice_index']):assert marker==fix['source_marker'];return fix['french_marker']
 return marker

def source_lines(name,review,passage,lines):
 row=packet(name,review);result=list(lines)
 if row is None:return result
 for fix in row['command_slots']:
  if fix['passage']==passage['_title']:
   i=fix['index'];assert result[i:i+len(fix['before'])]==fix['before'];result[i:i+len(fix['before'])]=fix['after']
 return result

def validate(name,review,english,french):
 row=packet(name,review)
 if row is None:return
 assert review.get('structural_alignment') and english['kind']==french['kind']=='compiled'
 assert english['original_file']==french['original_file']
 raw=read_json(ROOT/english['original_file']);assert raw['m_Name']==name;orig={r['lang']:r['dialog'] for r in raw['dialogs']}
 for lang,src,key in [(1,english,'english_sha256'),(0,french,'french_sha256')]:
  digest=hashlib.sha256(json.dumps(orig[lang],ensure_ascii=False,sort_keys=True).encode()).hexdigest();assert digest==src['source_sha256']==row[key]
  assert src['passages']==orig[lang]['_passages'],'Forged source object with old digest'
 assert orig[1]['_variables']==orig[0]['_variables']
 ep={p['_title']:p for p in english['passages']};fp={p['_title']:p for p in french['passages']};assert ep.keys()==fp.keys() and len(ep)==(60 if name.endswith('02') else 59)
 additions=review.get('additional_french_choices',[]);flags=review.get('condition_flag_overrides',[])
 if name.endswith('02'):
  assert len(additions)==len(flags)==1
  a=additions[0];assert set(a)=={'passage','english_insert_before','french_choice_index','russian','reason'}
  assert (a['passage'],a['english_insert_before'],a['french_choice_index'],a['russian'])==('mathieu-02-story2',2,2,'«Сломанное ребро».') and a['reason']
  flag=flags[0];assert (flag['passage'],flag['source_flag'],flag['french_flag'],flag['corroborating_file'],flag['corroborating_passage'])==('mathieu-02-start','_Mathieu.Talked.Talked.Father','_Mathieu.Talked.Father','original/DialogObjectScript/sharedassets0.assets/2262.json','mathieu-01-drive-talk') and flag['reason']
 else:assert not additions and not flags
 skeleton=lambda lines:[x if command(x) else '<TEXT>' for x in lines]
 for title,p in ep.items():
  q=fp[title];view=copy.deepcopy(p)
  for flag in flags:
   if flag['passage']==title:assert view['_link'].count(flag['source_flag'])==1;view['_link']=view['_link'].replace(flag['source_flag'],flag['french_flag'])
  assert {k:v for k,v in view.items() if k not in ['_lines','_choices']}=={k:v for k,v in q.items() if k not in ['_lines','_choices']}
  assert skeleton(source_lines(name,review,p,p['_lines']))==skeleton(q['_lines']),(name,title,'Unreviewed command timing')
  for i,c in enumerate(view['_choices']):
   target=expected_marker(review,title,i,emote(c['_text']));old=emote(c['_text']);c['_text']=c['_text'][len(old):].lstrip() if old else c['_text'];c['_text']=(target+' ' if target else '')+c['_text']
  for a in additions:
   if a['passage']==title:view['_choices'].insert(a['french_choice_index'],copy.deepcopy(q['_choices'][a['french_choice_index']]))
  assert len(view['_choices'])==len(q['_choices'])
  for a,b in zip(view['_choices'],q['_choices']):assert {k:v for k,v in a.items() if k!='_text'}=={k:v for k,v in b.items() if k!='_text'} and emote(a['_text'])==emote(b['_text'])
 for fix in row['markers']:
  a=ep[fix['passage']]['_choices'][fix['carrier_choice_index']];b=fp[fix['passage']]['_choices'][fix['french_choice_index']]
  assert a['_link']==b['_link'] and a['_ending']==b['_ending'] and emote(a['_text'])==fix['source_marker'] and emote(b['_text'])==fix['french_marker']
  assert fix['final_choice_index']==fix['french_choice_index']
