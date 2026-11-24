"""Save an explicitly selected, manually read French UI batch and exact fallbacks."""
from full_review import *

def save_ui_fr_batch(name,keys,edits,decisions,author,linked_contexts):
 assert len(keys)==len(set(keys)) and set(edits)<=set(keys)
 audit=read_json(DEST/'french_priority_audit.json');assert set(keys)<=set(audit['ui_pending'])
 frp=ROOT/'original/LocalizationData/sharedassets0.assets/2097.json'
 fr={x['_key']:x['_value'] for x in read_json(frp)['_items']};en=original_ui()
 up=UI_KEYS;ui=read_json(up)
 before={k:ui[k] for k in keys};ui.update(edits)
 mp=ROOT/'revision/data/Russian_UI/full_translation_mapping.json';mapping=read_json(mp);fallback=[];collisions=[]
 for k in edits:
  owners=[key for key,value in en.items() if value==en[k]]
  if {ui[key] for key in owners}!={ui[k]}:
   collisions.append(dict(key=k,english=en[k],owners=owners,decision='Keyed Russian is authoritative. Global legacy fallback cannot distinguish owners; unchanged.'))
   continue
  if mapping.get(en[k])!=ui[k]:
   fallback.append(dict(english=en[k],ui_keys=owners,previous_fallback=mapping.get(en[k]),russian=ui[k]));mapping[en[k]]=ui[k]
 # Synchronize prior explicit records without silently erasing their provenance.
 for p in (DEST/'contexts').glob('*_ui.json'):
  data=read_json(p);changed=False
  for item in data.get('keys',[]):
   if isinstance(item,dict) and item.get('key') in edits and 'russian' in item:
    k=item['key'];assert item['russian']==before[k],(p,k,'Stale context before batch')
    item.setdefault('russian_revision_history',[]).append(dict(author=author,before=item['russian'],after=ui[k]))
    item['russian']=ui[k];changed=True
  if changed:write_json(p,data)
 write_json(up,ui);write_json(mp,mapping)
 write_json(DEST/'contexts'/f'{name}_ui.json',dict(scope=f'{len(keys)}explicit French UI fields manually read/accepted; not whole interface.',french_priority_author=author,french_priority_decisions=decisions,linked_contexts_read=linked_contexts,keys=[dict(key=k,english=en[k],french=fr[k],russian=ui[k],reviewed=True,french_priority_reviewed=True,previous_contextual_russian=before[k],french_source_sha256=hashlib.sha256(frp.read_bytes()).hexdigest()) for k in keys],exact_legacy_fallback_updates=fallback,legacy_fallback_owner_collisions=collisions))
 print('Accepted',len(keys),'FR UI keys;',sum(ui[k]!=before[k] for k in keys),'actual changes;',len(fallback),'exact fallbacks;',len(collisions),'owner collisions')
