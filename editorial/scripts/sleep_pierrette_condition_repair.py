"""One proven shared-original flag spelling mismatch; no new dream entrances."""
import hashlib,json
from corpus import ROOT,read_json
NAME='930_sleep_eng';FIELD='sleep_pierrette_condition_repair'
CASE=dict(passage='sleep-client-day4-hub',before='-> _Driver.Met.Pierette = 1 ? sleep-client-072pierrette',after='-> _Driver.Met.Pierrette = 1 ? sleep-client-072pierrette',english_sha256='b4b09448d009aff29f42084c8fbac2ea3cde4921d60a338d7be902964dc5ce6a',french_sha256='c3d075947ae93d72441306295ec42a45fc63501f87511824ad9c76596c159c02',writer_file='original/DialogObjectScript/sharedassets0.assets/2270.json',writer_french_sha256='5b48473ea9dab2227f172195f9bfe8c6b90ab25eaab3eb49607916a6308f0ef8',writer_passage='pierrette-01-start',writer_modifier='_Driver.Met.Pierrette=1')
def packet(name,review):
 row=(review or {}).get(FIELD)
 if name!=NAME:assert row is None;return None
 assert isinstance(row,dict) and set(row)==set(CASE)|{'reason'}
 assert all(row[k]==v and type(row[k]) is type(v) for k,v in CASE.items())
 assert isinstance(row['reason'],str) and row['reason'].strip();return row
def replace_condition(content):
 title=None;count=0;lines=content.splitlines(keepends=True)
 for i,line in enumerate(lines):
  v=line.strip()
  if v.startswith('==='):title=v[3:].strip()
  elif title==CASE['passage'] and v==CASE['before']:
   assert line.count('_Driver.Met.Pierette')==1
   lines[i]=line.replace('_Driver.Met.Pierette','_Driver.Met.Pierrette',1);count+=1
 assert count==1,'Exactly one pinned day4 condition required';return ''.join(lines)
def validate(name,review,english,french):
 if packet(name,review) is None:return
 assert english['kind']==french['kind']=='script'
 for record,key in [(english,'english_sha256'),(french,'french_sha256')]:
  raw=read_json(ROOT/record['original_file'])['m_Script']
  assert hashlib.sha256(raw.encode('utf-8')).hexdigest()==record['source_sha256']==CASE[key]
  if key=='english_sha256':assert english['script']==raw
  assert replace_condition(raw).count(CASE['after'])==1
 writer=read_json(ROOT/CASE['writer_file']);assert writer['m_Name']=='072_pierrette_01'
 fr=next(r['dialog'] for r in writer['dialogs'] if r['lang']==0)
 assert hashlib.sha256(json.dumps(fr,ensure_ascii=False,sort_keys=True).encode('utf-8')).hexdigest()==CASE['writer_french_sha256']
 p=next(p for p in fr['_passages'] if p['_title']==CASE['writer_passage'])
 assert p['_variable_modifiers']==[CASE['writer_modifier']]
def apply_repair(source,reviewed,content):
 if source['name']!=NAME:return content
 review=reviewed['french_review']
 validate(NAME,review,source,dict(kind='script',original_file=review['original_file'],source_sha256=review['source_sha256']))
 return replace_condition(content)
