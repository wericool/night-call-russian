"""One proven original typo at the explicit stopped-and-paid narration boundary."""
import hashlib,json
from corpus import ROOT,read_json,command
NAME='045_vero_01';FIELD='vero_travel_command_repair'
CASE=dict(passage='vero-01-end-silence',index=2,before='$$ trave: stop $$',after='$$ travel: stop $$',english_sha256='191d6c3c139070c4b7a26f6249ea7eaa43a6c5e3f3504409210a010cc8697f01',french_sha256='90b1449d62818a2bf2780b316ea56233455ab5c739561e5c28b8c9b01e07bd26')
def packet(name,review):
 row=(review or {}).get(FIELD)
 if name!=NAME:assert row is None;return None
 assert isinstance(row,dict) and set(row)==set(CASE)|{'reason'}
 assert all(row[k]==v and type(row[k]) is type(v) for k,v in CASE.items())
 assert isinstance(row['reason'],str) and row['reason'].strip()
 return row
def source_lines(name,review,passage,lines):
 row=packet(name,review);result=list(lines)
 if row is not None and passage['_title']==CASE['passage']:
  assert result[CASE['index']]==CASE['before'] and len(result)==6
  result[CASE['index']]=CASE['after']
 return result
def validate(name,review,english,french):
 if packet(name,review) is None:return
 assert english['kind']==french['kind']=='compiled' and english['original_file']==french['original_file']
 raw=read_json(ROOT/english['original_file']);assert raw['m_Name']==NAME
 source={r['lang']:r['dialog'] for r in raw['dialogs']}
 for record,lang,key in [(english,1,'english_sha256'),(french,0,'french_sha256')]:
  assert hashlib.sha256(json.dumps(source[lang],ensure_ascii=False,sort_keys=True).encode('utf8')).hexdigest()==record['source_sha256']==CASE[key]
  assert record['passages']==source[lang]['_passages']
  p=next(p for p in record['passages'] if p['_title']==CASE['passage'])
  assert p['_lines'][2]==CASE['before'] and p['_link']=='vero-01-end-final'
  assert p['_lines'][4]=='$$ movie: lamparo_off, hide_passenger $$'
  assert not any('$$ travel: stop $$'==s for q in record['passages'] for s in q['_lines'])
 fp=next(p for p in french['passages'] if p['_title']==CASE['passage'])
 assert fp['_lines'][3].startswith("À peine à l'arrêt, la cliente vous tend le prix de la course.")
 ep={p['_title']:p for p in english['passages']};fr={p['_title']:p for p in french['passages']}
 assert ep.keys()==fr.keys()
 skeleton=lambda ls:[x if command(x) else '<TEXT>' for x in ls]
 for title,p in ep.items():
  target=list(fr[title]['_lines'])
  if title==CASE['passage']:target[2]=CASE['after']
  assert skeleton(source_lines(name,review,p,p['_lines']))==skeleton(target)
