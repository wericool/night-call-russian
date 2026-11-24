"""Current keyed UI diff for reports; keep the historical editorial ledger intact."""
from corpus import *
import hashlib

def current_ui_changes(historical,french_keys):
 baseline_path=ROOT/'upstream/data/Russian_UI/key_based_translations.json'
 current_path=UI_KEYS
 baseline=read_json(baseline_path);current=read_json(current_path);english=original_ui()
 from native_ui_extensions import reviewed_extensions
 extensions=reviewed_extensions(current);extension_by_key={x['key']:x for x in extensions}
 english.update({x['key']:x['english'] for x in extensions})
 assert set(english)<=set(current)
 old={x['key']:x for x in historical if x['category']=='ui'}
 sources={x['key']:p.relative_to(ROOT).as_posix() for p in (ROOT/'localization/contexts').glob('*_ui.json') for x in read_json(p).get('keys',[]) if isinstance(x,dict) and x.get('french_priority_reviewed') is True}
 result=[]
 for key,en in english.items():
  before=baseline.get(key,'');after=current[key]
  if key in baseline and before==after:continue
  if key not in baseline and not after:continue
  item=dict(old.get(key,{}))
  item.update(category='ui',key=key,english=en,before=before,after=after,baseline='upstream/data/Russian_UI/key_based_translations.json',before_missing=key not in baseline)
  if key in extension_by_key:
   x=extension_by_key[key];item['french']=x['french_evidence'];item['source_kind']=x['source_kind']
   item['reason']='Добавлено отсутствующее поле уже существующей улики Судьи09. Смысл сверён с французской статьёй и исходным ресурсом улики; это два новых нативных ключа, а не псевдонимы или новая улика. Доказательства: localization/native_ui_extensions.json и NEWSPAPER_CLUE_UI_RU.md.'
  elif key in french_keys:
   item['reason']='Текущая редакция поля интерфейса; полная ручная сверка по французскому оригиналу принята. Подробные решения: '+sources[key]+'.'
  elif key not in old:
   item['reason']='Текущее отличие рабочей таблицы интерфейса от исходной таблицы по ключам. Запись фиксирует текст; окончательная сверка по французскому приоритету ещё не принята.'
  result.append(item)
 assert len({x['key'] for x in result})==len(result)
 assert all(x['after']==current[x['key']] and x['before']==baseline.get(x['key'],'') for x in result)
 expected={k for k in english if baseline.get(k,'')!=current[k]}
 assert {x['key'] for x in result}==expected
 write_json(ROOT/'review/current_ui_changes.json',result)
 sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
 write_json(ROOT/'review/ui_changelog_snapshot.json',dict(passed=True,scope='Exact current keyed-table differences for921originalUI keys plus separately source-backed native extensions; not visual gameplay acceptance. Missing baseline key is explicit.',changed_keys=len(result),original_keys=len(english)-len(extensions),source_backed_extension_keys=sorted(extension_by_key),baseline_sha256=sha(baseline_path),current_sha256=sha(current_path),current_ui_diff_sha256=sha(ROOT/'review/current_ui_changes.json'),superseded_stale_ledger_keys=[k for k,v in old.items() if k in current and v['after']!=current[k]],newly_recorded_keys=sorted({x['key'] for x in result}-set(old)),unknown_working_keys=sorted(set(current)-set(english)),historical_ledger_preserved=True))
 return [x for x in historical if x['category']!='ui']+result
