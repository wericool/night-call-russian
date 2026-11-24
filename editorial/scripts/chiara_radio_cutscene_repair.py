"""A missing cutscene key in both originals; retain the intended radio view."""
import hashlib,json
from corpus import ROOT,read_json,command
NAME='033_chiara_01';FIELD='chiara_radio_cutscene_repair'
CASE=dict(passage='chiara-01-leaves',index=3,before='$$ movie: Radio_Loop $$',after='$$ movie: LightsOnRadio_Loop $$',english_sha256='f4bdab604c82dd4863571717865a77c3a80393cd8d74a7de492bafc48f503ed1',french_sha256='531e4e7d66c2394d0a2bc74f7828be8b06ea235c6a9b298834e058c027394368',cutscene_file='original/CutsceneScript/sharedassets0.assets/2033.json',holder_file='original/CutsceneHolderScript/sharedassets0.assets/2083.json')

def packet(name,review):
 row=(review or {}).get(FIELD)
 if name!=NAME:assert row is None;return None
 assert isinstance(row,dict) and set(row)==set(CASE)|{'reason'}
 assert all(row[k]==v and type(row[k]) is type(v) for k,v in CASE.items())
 assert row['reason'].strip();return row

def source_lines(name,review,passage,lines):
 row=packet(name,review);result=list(lines)
 if row is not None and passage['_title']==CASE['passage']:
  assert result[CASE['index']]==CASE['before'] and result[0]=='$$ travel: stop $$'
  result[CASE['index']]=CASE['after']
 return result

def validate(name,review,english,french):
 row=packet(name,review)
 if row is None:return
 assert english['kind']==french['kind']=='compiled' and english['original_file']==french['original_file']
 raw=read_json(ROOT/english['original_file']);src={r['lang']:r['dialog'] for r in raw['dialogs']}
 for record,lang,key in [(english,1,'english_sha256'),(french,0,'french_sha256')]:
  assert hashlib.sha256(json.dumps(src[lang],ensure_ascii=False,sort_keys=True).encode('utf8')).hexdigest()==record['source_sha256']==CASE[key]
  assert record['passages']==src[lang]['_passages']
  p=next(p for p in record['passages'] if p['_title']==CASE['passage']);assert p['_lines'][3]==CASE['before'] and p['_lines'][0]=='$$ travel: stop $$'
 holder=read_json(ROOT/CASE['holder_file']);ids={r['m_PathID'] for r in holder['ingame_cutscenes'] if r['m_FileID']==0};assert 2033 in ids
 records=[read_json(ROOT/'original/CutsceneScript/sharedassets0.assets'/f'{i}.json') for i in ids]
 keys=[r['key'].lower() for r in records];assert 'radio_loop' not in keys and keys.count('lightsonradio_loop')==1
 target=read_json(ROOT/CASE['cutscene_file']);assert target['key']=='LightsOnRadio_Loop' and target['media']==dict(m_FileID=0,m_PathID=1667)
 ep=next(p for p in english['passages'] if p['_title']==CASE['passage']);fp=next(p for p in french['passages'] if p['_title']==CASE['passage']);desired=list(fp['_lines']);desired[3]=CASE['after'];skeleton=lambda ls:[x if command(x) else '<TEXT>' for x in ls]
 assert skeleton(source_lines(name,review,ep,ep['_lines']))==skeleton(desired)
