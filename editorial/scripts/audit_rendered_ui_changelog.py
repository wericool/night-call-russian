"""Compare each rendered current UI before/after block against the actual tables."""
from corpus import *
import hashlib

def main():
 path=ROOT/'CHANGELOG_RU.md';text=path.read_text(encoding='utf-8-sig')
 parts=re.split(r'^### \d+\. (.+)\n',text,flags=re.M)
 entries={title:body for title,body in zip(parts[1::2],parts[2::2])}
 rows=read_json(ROOT/'review/current_ui_changes.json')
 base=read_json(ROOT/'upstream/data/Russian_UI/key_based_translations.json');current=read_json(UI_KEYS);en=original_ui()
 from native_ui_extensions import reviewed_extensions
 en.update({x['key']:x['english'] for x in reviewed_extensions(current)})
 expected={k for k in en if base.get(k,'')!=current[k]};assert {x['key'] for x in rows}==expected
 for row in rows:
  key=row['key'];body=entries[key]
  for label,value in [('Было',base.get(key,'')),('Стало',current[key])]:
   m=re.search(r'^'+label+r':\n\n```text\n(.*?)\n```',body,flags=re.M|re.S)
   assert m and m.group(1)==(value or '(пусто)'),(key,label)
  if key not in base:assert 'В исходной таблице переводов этого ключа не было.' in body,key
 sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
 write_json(ROOT/'review/rendered_ui_changelog_validation.json',dict(passed=True,checked_keys=len(rows),changelog_sha256=sha(path),current_ui_sha256=sha(UI_KEYS),current_ui_diff_sha256=sha(ROOT/'review/current_ui_changes.json'),baseline_sha256=sha(ROOT/'upstream/data/Russian_UI/key_based_translations.json'),scope='Every original-keyed-table difference has exact baseline/current Russian text in the generated Markdown. Empty values and absent old keys checked. Not semantic acceptance of pending UI or Unity display.'))
 print('Rendered UI changelog:',len(rows),'exact before/after pairs match current and baseline tables.')

if __name__=='__main__':main()
