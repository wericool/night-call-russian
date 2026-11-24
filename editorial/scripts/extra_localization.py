"""Source-bound editorial data outside the 236 dialogue scenes."""
from pathlib import Path
import json,hashlib,re
from corpus import REVIEWED
ROOT=Path(__file__).resolve().parents[1]
PASSIDEX=ROOT/'localization/passidex_reviewed.json'
CREDITS=ROOT/'localization/credits_reviewed.json'

def read(p):return json.loads(Path(p).read_text(encoding='utf-8-sig'))
def sha(p):return hashlib.sha256(Path(p).read_bytes()).hexdigest()

def check_rules(rows,rules):
 result=[];byid={r['reveal_id']:r for r in rows}
 for rule in rules.get('external_text_rules',[]):
  for rid in rule['reveal_ids']:
   assert rid in byid,('Unknown external rule ID',rule['key'],rid)
   text=byid[rid]['russian']
   missing=[p for p in rule['preferred_patterns'] if not re.search(p,text,re.I)]
   forbidden=[p for p in rule['forbidden_patterns'] if re.search(p,text,re.I)]
   if missing or forbidden:
    result.append(dict(scene='@passidex',passage=rid,rule=rule['key'],issue='external_text_consistency',status='reviewed_conflict',russian=text,missing=missing,forbidden=forbidden))
 return result

def load_passidex(rules=None,check_consistency=True,data=None):
 d=data if data is not None else read(PASSIDEX);assert d['schema_version']==1 and d['primary_language']=='fr' and d['secondary_language']=='en'
 assert sha(ROOT/d['source_file'])==d['source_sha256']
 assert sha(ROOT/d['prior_full_audit'])==d['prior_full_audit_sha256']
 original=read(ROOT/d['source_file'])['reveals'];assert len(original)==len(d['rows'])==481
 assert len({r['reveal_id'] for r in d['rows']})==481
 for i,(s,r) in enumerate(zip(original,d['rows'])):
  assert r['index']==i and r['reveal_id']==s['reveal_id'] and r['passenger_id']==s['passenger_id']
  assert r['french']==s['text'][0] and r['english_auxiliary']==s['text'][1]
  assert r['status']=='french_reviewed' and isinstance(r['russian'],str) and r['russian'].strip()
  assert '\n' not in r['russian'] and not r['russian'].startswith(('$$','===','->','%%'))
 for c in d['context_sources']:
  assert sha(REVIEWED/Path(c['reviewed_file']).name)==c['reviewed_sha256'],('Recheck related context',c['scene'])
  assert sha(ROOT/c['french_original_file'])==c['french_file_sha256']
 if check_consistency:
  conflicts=check_rules(d['rows'],rules or read(ROOT/'localization/consistency.json'))
  assert not conflicts,conflicts
 return d

def load_credits(data=None):
 d=data if data is not None else read(CREDITS);assert d['schema_version']==1
 assert sha(ROOT/d['baseline_package']/'manifest.json')==d['baseline_manifest_sha256']
 assert sha(ROOT/d['prior_readback'])==d['prior_readback_sha256']
 allowed={('level3',1494),('level3',1553),('level3',1599),('level3',1635),('level7',835),('level7',892),('level7',938),('level7',970)}
 assert {(r['asset'],r['path_id']) for r in d['rows']}==allowed and len(d['rows'])==8
 prior=read(ROOT/d['prior_readback'])
 for r in d['rows']:
  old=next(x['text'] for x in prior['texts'] if x['file']==r['asset'] and x['id']==r['path_id'])
  changes=next(x for x in prior['files'] if x['file']==r['asset'])['changed']
  changed=next((x for x in changes if x['id']==r['path_id']),None)
  source=next(x['before'] for x in changed['differences'] if x['path']=='/m_text') if changed else old
  assert r['before']==old and r['source_original']==source,'Credit source or carrier changed'
  assert r['source_language'] in ('fr','shared_original_english') and r['reason']
  assert r['source_original'] and r['russian'] and r['before']!=r['russian']
  assert re.findall('<[^>]+>',r['before'])==re.findall('<[^>]+>',r['russian']),'Rich text markup changed'
  if r['source_language']=='shared_original_english':
   assert r['source_original'].startswith('Anaïs Mayance') or r['source_original']=='<u>Production Baby</u>'
  if r['source_original'].startswith('Anaïs Mayance'):
   assert r['russian'].startswith('Anaïs Mayance<size=30>') and r['russian'].endswith('(Damien)<i></size>')
 return d

def consistency_findings(rules):
 if not PASSIDEX.exists():return []
 d=load_passidex(check_consistency=False)
 return check_rules(d['rows'],rules)

def guard_probes(rules):
 """Owner/ID-scoped corruptions and valid alternatives, not duplicated translations."""
 import copy
 d=load_passidex(rules);rows=d['rows'];results=[]
 cases=[('026_02','Вы нашли Ультра Жёлтого перед отелем.',True),('026_03','Вы нашли Ультра Жёлтую перед Furiosa.',True),('075_01','Вы выставили Анри и Муйетта из такси.',True),('018_03','Франсуа написал книгу «Покорённая Франция».',True),('018_04','Франсуа написал книгу «Монархист, и что с того?»',True),('052_02','Вы вспомнили о существовании Гравиасов.',True),('073_05','Мириам рассказала о своей влюблённости в отца…',True),('052_01','Вы играли с Шильдериком, как только могли.',True),('070_02','Антуан выиграл в лото.',True),('018_08','Вы доехали за Франсуа до New York Pizza Pizza.',True),('075_01','Вы выставили Анри и его собаку Муйетт из такси.',False),('026_04','У парка вы нашли Ультра Жёлтую.',False),('023_01','Вы привезли Аполлони в «Фуриозу».',False),('052_02','Вы вспомнили о гравиях.',False),('073_05','Мириам рассказала, что влюблена в отца ребёнка…',False),('052_01','Вы во всём подыгрывали Шильдерику.',False),('018_08','Вы наблюдали за Франсуа у New York Pizza Pizza.',False),('000_00','Мужчина в жёлтом пальто проехал мимо Furiosa.',False),('000_07','Мой знакомый выиграл в лото.',False),('000_08','В другой книге Муйетта — имя мужчины.',False)]
 for rid,text,bad in cases:
  changed=copy.deepcopy(rows);next(r for r in changed if r['reveal_id']==rid)['russian']=text
  actual=bool(check_rules(changed,rules));results.append(dict(rule='external_id_scope',text=text,reveal_id=rid,expected_conflict=bad,passed=actual==bad))
 for label in ['missing_row','duplicate_id','reordered','french_changed','empty_target','wrong_primary']:
  x=copy.deepcopy(d)
  if label=='missing_row':x['rows'].pop()
  elif label=='duplicate_id':x['rows'][1]['reveal_id']=x['rows'][0]['reveal_id']
  elif label=='reordered':x['rows'][0],x['rows'][1]=x['rows'][1],x['rows'][0]
  elif label=='french_changed':x['rows'][0]['french']='invented source'
  elif label=='empty_target':x['rows'][0]['russian']=''
  else:x['primary_language']='en'
  try:load_passidex(rules,data=x)
  except AssertionError:passed=True
  else:passed=False
  results.append(dict(rule='external_source_integrity',text=label,expected_conflict=True,passed=passed))
 credits=load_credits()
 for label in ['missing_row','unknown_object','markup_changed','person_renamed','source_changed']:
  x=copy.deepcopy(credits)
  if label=='missing_row':x['rows'].pop()
  elif label=='unknown_object':x['rows'][0]['path_id']=999999
  elif label=='markup_changed':x['rows'][0]['russian']+='<b>'
  elif label=='person_renamed':
   r=next(r for r in x['rows'] if r['source_original'].startswith('Anaïs Mayance'));r['russian']=r['russian'].replace('Anaïs','Anais')
  else:x['rows'][0]['source_original']='invented source'
  try:load_credits(data=x)
  except AssertionError:passed=True
  else:passed=False
  results.append(dict(rule='external_credit_integrity',text=label,expected_conflict=True,passed=passed))
 return results

def write_extra_reports():
 if not PASSIDEX.exists():return
 d=load_passidex();c=load_credits()
 lines=['# Пассидекс и титры: редактура от 18.10.2026','',
  'Исправлены 22 записи Пассидекса и восемь полей титров. Источники и решения сохранены по reveal_id и asset/pathID. Это редакторская работа; состояние нативной сборки и живой проверки фиксируется отдельно.','',
  '## Пассидекс','','| ID | Французский | Было | Стало | Почему |','|---|---|---|---|---|']
 def cell(s):return str(s).replace('|','\\|').replace('\n','<br>')
 for r in d['rows']:
  if 'before' in r:lines.append('| '+' | '.join(cell(x) for x in [r['reveal_id'],r['french'],r['before'],r['russian'],r['decision']])+' |')
 lines += ['', '## Титры', '', '| Ресурс / ID | Источник | Было | Стало | Почему |','|---|---|---|---|---|']
 for r in c['rows']:
  lines.append('| '+' | '.join(cell(x) for x in [r['asset']+'/'+str(r['path_id']),r['source_original'],r['before'],r['russian'],r['reason']])+' |')
 text='\n'.join(lines)+'\n';(ROOT/'EXTRA_TEXT_CHANGES_RU.md').write_text(text,encoding='utf-8')
 with (ROOT/'CHANGELOG_RU.md').open('a',encoding='utf-8') as f:f.write('\n\n'+text.replace('# Пассидекс','## Пассидекс',1))
