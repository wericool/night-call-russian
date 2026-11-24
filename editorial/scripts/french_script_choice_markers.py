"""Five source-pinned French choice markers in three original TextAssets."""
import hashlib
from corpus import ROOT,read_json,parse_text_content,emote
from sleep_french_navigation import skeleton
FIELD='french_script_choice_marker_restorations'
CASES={
 '980_Judge_01_Location_eng':dict(english_sha256='540634d5d947e4cad74450dc4fd5532c2bc0f122ae152cfa9813d36e217fcc62',french_sha256='1c47ad8fb0b554835eafc598c7ff595acac7ebdd3aa1dfe2824a1e4df0456f4d',choices=[('judge-crimescene01-entrance-intro',3,':taxi:')]),
 '981_Angel_01_Location_eng':dict(english_sha256='badfce90be430e027d4a884454bd816fe6d4aee8f69a63474d2213a35cc4745b',french_sha256='c6d1a29fc70b3c1cdeac686037078552587fc239c7ac54e867b2134fb37c5506',choices=[('981-01-end-intro',2,':silence:'),('981-01-visit-answer',3,':silence:'),('981-01-why',2,':silence:')]),
 '981_Angel_02_Location_eng':dict(english_sha256='dd12f1678139610cb1ac2c188ecd0d0f4eb3e112123dada235c0cb79b738104f',french_sha256='586623f55036a9631a36b98043871d8c14e9a27169b98ffd6f379306677c813c',choices=[('981-02-tanguy',3,':silence:')])}

def expected_marker(review,title,index,source_marker):
 for f in (review or {}).get(FIELD,[]):
  if (f['passage'],f['choice_index'])==(title,index):
   assert source_marker==f['source_marker'];return f['french_marker']
 return source_marker

def validate(name,review,english,french):
 fixes=review.get(FIELD,[]);assert isinstance(fixes,list)
 if name not in CASES:assert not fixes;return
 case=CASES[name];assert english['kind']==french['kind']=='script' and len(fixes)==len(case['choices'])
 raws=[]
 for src,key in [(english,'english_sha256'),(french,'french_sha256')]:
  raw=read_json(ROOT/src['original_file'])['m_Script'];assert hashlib.sha256(raw.encode('utf8')).hexdigest()==src['source_sha256']==case[key]
  blocks,duplicates=parse_text_content(raw);assert not duplicates
  passages=[dict(_title=b['title'],_lines=b['lines'],_choices=[dict(_text=c['text'],_link=c['link']) for c in b['choices']]) for b in blocks.values()]
  assert passages==src['passages'],'Source object must equal immutable TextAsset'
  raws.append(raw)
 assert english['script']==raws[0]
 triggers={r['trigger_name'] for r in read_json(ROOT/'review/original_choice_emoticon_config.json')['configuration']}
 for f,(title,index,marker) in zip(fixes,case['choices']):
  assert set(f)=={'passage','choice_index','source_marker','french_marker','reason'}
  assert type(f['choice_index']) is int and (f['passage'],f['choice_index'],f['source_marker'],f['french_marker'])==(title,index,'',marker)
  assert f['reason'].strip() and marker.strip(':') in triggers
  for src,expected in [(english,''),(french,marker)]:assert emote(next(p for p in src['passages'] if p['_title']==title)['_choices'][index]['_text'])==expected
 lines=raws[0].splitlines(keepends=True);title=None;index=0;seen=set()
 for i,line in enumerate(lines):
  s=line.strip()
  if s.startswith('==='):title=s[3:].strip();index=0
  elif s.startswith('*'):
   marker=expected_marker(review,title,index,'')
   if marker:assert line.count('* ')==1;lines[i]=line.replace('* ','* '+marker+' ',1);seen.add((title,index))
   index+=1
 assert seen=={(t,i) for t,i,_ in case['choices']}
 assert skeleton(''.join(lines))==skeleton(raws[1]),'Only exact French markers may differ'
