"""Inventory explicit per-key UI reviews without inferring semantic completeness."""
import hashlib
from collections import Counter, defaultdict
from full_review import *


def main():
    ui=read_json(UI_KEYS)
    english=original_ui();records=defaultdict(list);mismatches=[];inputs={}
    for path in sorted((DEST/'contexts').glob('*_ui.json')):
        data=read_json(path);relative=path.relative_to(ROOT).as_posix()
        inputs[relative]=hashlib.sha256(path.read_bytes()).hexdigest()
        for item in data.get('keys',[]):
            if not isinstance(item,dict) or 'key' not in item or 'russian' not in item:continue
            key=item['key'];records[key].append(dict(context=relative,russian=item['russian']))
            if ui.get(key)!=item['russian']:
                mismatches.append(dict(key=key,context=relative,expected=item['russian'],actual=ui.get(key)))
    remaining=[dict(key=key,english=value,russian=ui.get(key),status='no_explicit_context_record_in_contexts_ui_files')
               for key,value in english.items() if key not in records]
    inputs['revision/data/Russian_UI/key_based_translations.json']=hashlib.sha256((UI_KEYS).read_bytes()).hexdigest()
    write_json(DEST/'ui_editorial_coverage.json',dict(
        scope='Only explicit key/russian records in contexts/*_ui.json. Older dossier, menu or other editorial decisions can be recorded elsewhere; remaining is an audit queue, not a confirmed error or unreviewed verdict.',
        total_keys=len(english),explicit_keys=len(records),mismatched_records=mismatches,
        remaining=remaining,records=dict(records),input_sha256=inputs))
    print('Explicit UI records:',len(records),'of',len(english),'keys;',len(mismatches),'stale qualified values.')
    print('Audit queue categories:',dict(Counter(x['key'].split('.')[0].split('_')[0] for x in remaining)))
    assert not mismatches,mismatches


if __name__=='__main__':main()
