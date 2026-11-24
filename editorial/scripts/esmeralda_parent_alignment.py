"""Two exact FR command/text boundaries; preserve the proven reverse animation."""
import hashlib,json
from corpus import ROOT,read_json,command
NAME='006_esmeralda_01';PASSAGE='esmeralda-01-parents'
FIELD='french_esmeralda_parent_alignment'
CASE=dict(passage=PASSAGE,english_sha256='e0b3a4aee59e5fd6ae716a2e8ded08d6408bddb5206b954701133d997855bdfb',french_sha256='07073dde2cc1fa9750d3cb80a2bb831b8b817c9dcfd95704d043a7b9bab28d26',move_fast_music_after_text_index=6,move_normal_before_text_index=11,retained_animation_index=14,retained_animation='$$ anim: GETTING_ANGRY_REV $$',french_animation='$$ anim: GETTING_ANGRY $$',animation_proof='review/esmeralda_animation_20261027/animation_proof.json',animation_proof_sha256='52626686c91668bb911e487aa6175766217396ad80dc7a6db070d88451dc3e25')

def packet(name,review):
 row=(review or {}).get(FIELD)
 if name!=NAME:assert row is None;return None
 assert isinstance(row,dict) and set(row)==set(CASE)|{'reason'}
 assert all(row[k]==v and type(row[k]) is type(v) for k,v in CASE.items())
 assert isinstance(row['reason'],str) and row['reason'].strip()
 return row

def source_lines(name,review,passage,lines):
 row=packet(name,review);out=list(lines)
 if row is None or passage['_title']!=PASSAGE:return out
 assert out[4:6]==['$$ dialogspeed: fast $$','$$ music-control: 40 $$'] and not command(out[6])
 assert not command(out[11]) and out[12]=='$$ dialogspeed: normal $$'
 assert out[14:16]==[CASE['retained_animation'],'$$ anim: IDLE_TELEPHONE $$']
 out[4:7]=[out[6],out[4],out[5]]
 out[11:13]=[out[12],out[11]]
 return out

def validate(name,review,english,french):
 row=packet(name,review)
 if row is None:return
 assert english['kind']==french['kind']=='compiled' and english['original_file']==french['original_file']=='original/DialogObjectScript/sharedassets0.assets/2136.json'
 raw=read_json(ROOT/english['original_file']);originals={r['lang']:r['dialog'] for r in raw['dialogs']}
 for src,lang,key in [(english,1,'english_sha256'),(french,0,'french_sha256')]:
  assert hashlib.sha256(json.dumps(originals[lang],ensure_ascii=False,sort_keys=True).encode('utf8')).hexdigest()==src['source_sha256']==CASE[key]
  assert src['passages']==originals[lang]['_passages'],'Forged source object with old digest'
 assert originals[0]['_variables']==originals[1]['_variables']
 proof=ROOT/CASE['animation_proof']
 # The public evidence omits the local backup path. The review retains the original evidence hash.
 assert hashlib.sha256(proof.read_bytes()).hexdigest()=='79913017cca45a3c8acb6176d202cd5b5d1f7163119f5c8a94859f8debeb5f90'
 p=read_json(proof)
 assert p['passed'] and p['motion_and_bindings_identical'] and p['forward']['speed']==1 and p['reverse']['speed']==-1
 assert p['installed_animation_objects_equal_pristine']
 ep=next(p for p in english['passages'] if p['_title']==PASSAGE);fp=next(p for p in french['passages'] if p['_title']==PASSAGE)
 normalize=lambda x:x.replace('\u00a0',' ') if command(x) else '<TEXT>'
 desired=[normalize(x) for x in fp['_lines']];assert fp['_lines'][14]==CASE['french_animation'];desired[14]=CASE['retained_animation']
 assert [normalize(x) for x in source_lines(name,review,ep,ep['_lines'])]==desired,'Other parent timing/command differences need separate review'
 speech=next(p for p in french['passages'] if p['_title']=='esmeralda-01-speech')['_lines'];i=speech.index(CASE['retained_animation']);assert speech[i+1]=='$$ anim: IDLE_TELEPHONE $$'
