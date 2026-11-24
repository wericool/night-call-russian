"""Five named FR target restorations; never relax the missing-target exception."""
from corpus import ROOT,read_json,emote
from copy import deepcopy
from functools import lru_cache
from pathlib import Path
import hashlib,json,re

CASES={
 '017_grace_01':dict(english_sha256='ed3271844f73406134eddc1c6561630928b15e8514fa837fb1418e5a36fad7d5',french_sha256='53f4b7e45818e6be67f691c1ff18049833edb7658d593b87d429908a4c188d64',choices=[('grace-01-silence01',2,'grace-01-end','grace-01-end-bad')]),
 '018_francois_07':dict(english_sha256='ef3ee109c802a24f141d3c34f29f901682d9332574bad9911c971dc23ef2d5d4',french_sha256='c01f54c03b1e176f1fc86c836a582667beb5a92905149aff447099b48c716ff1',choices=[('francois-07-yes',1,'francois-07-silence','francois-07-silent')]),
 '020_jacquie_01':dict(english_sha256='155932235f2ef284a523e2e59bf5d0fb350a12edc37413be8ee151fa7d84296d',french_sha256='002cc913b7d3ab731a659fb56333c4ac437d2a49dd806dad8dde32b7d6fe3e7f',choices=[('jacquie-01-story3',2,'jacquie-01-story-end','jacquie-01-story-end-intro')]),
 '060_mireille_01':dict(english_sha256='b8367be1492e01d2b3e602300c5677b5161704ea16d4432e2462c2468250cd08',french_sha256='9e4996ef77842df64df92574f1fbdd0eebf48cdcfaf856d93fd94c0d281ce882',choices=[('mireille-01-part2-intro2',3,'mireille-01-part2-intro2-refused','mireille-01-part2-intro2-refuse'),('mireille-01-part2-intro2-sure',2,'mireille-01-part2-intro2-refused','mireille-01-part2-intro2-refuse')])
}

def validate_source_graph_alignment(name,english,french):
 """The complete original graphs differ only in the five approved choice slots."""
 allowed=CASES[name];en={p['_title']:p for p in english['passages']};fr={p['_title']:p for p in french['passages']};assert en.keys()==fr.keys()
 fixes={(t,i):(old,new) for t,i,old,new in allowed['choices']}
 for title,a in en.items():
  b=fr[title]
  assert {k:v for k,v in a.items() if k not in {'_lines','_choices'}}=={k:v for k,v in b.items() if k not in {'_lines','_choices'}}
  # These immutable originals have identical command positions. Formatting
  # whitespace differs, but no command token, order or placement may change.
  positions=lambda lines:[(i,re.sub(r'\s+','',x)) for i,x in enumerate(lines) if x.strip().startswith('$$')]
  assert positions(a['_lines'])==positions(b['_lines'])
  assert len(a['_lines'])==len(b['_lines']) and len(a['_choices'])==len(b['_choices'])
  for i,(x,y) in enumerate(zip(a['_choices'],b['_choices'])):
   original=deepcopy(x)
   if (title,i) in fixes:
    old,new=fixes[(title,i)];assert x['_link']==old and y['_link']==new;original['_link']=new
   assert {k:v for k,v in original.items() if k!='_text'}=={k:v for k,v in y.items() if k!='_text'}
   assert emote(x['_text'])==emote(y['_text'])

def validate_choice_target_restorations(name,review,english,french):
 fixes=review.get('choice_target_restorations',[]);assert isinstance(fixes,list)
 allowed=CASES.get(name)
 if allowed is None:
  assert not fixes,(name,'No permission for an existing-target restoration in this scene')
  return
 assert english['name']==french['name']==name and english['kind']==french['kind']=='compiled'
 assert english['source_sha256']==allowed['english_sha256'] and french['source_sha256']==allowed['french_sha256']
 assert review.get('source_sha256')==allowed['french_sha256'] and review.get('structural_alignment')
 assert len(fixes)==len(allowed['choices']),(name,'Every named French target restoration is mandatory')
 for other in ['choice_link_overrides','additional_french_choices','additional_french_lines','condition_flag_overrides']:
  assert not review.get(other,[]),(name,'These five restorations cannot hide a second kind of graph edit',other)
 en={p['_title']:p for p in english['passages']};fr={p['_title']:p for p in french['passages']};expected=set(allowed['choices']);seen=set();menus=set()
 for fix in fixes:
  assert set(fix)=={'passage','choice_index','english_target','french_target','reason'}
  assert type(fix['choice_index']) is int and isinstance(fix['reason'],str) and fix['reason'].strip()
  key=(fix['passage'],fix['choice_index'],fix['english_target'],fix['french_target']);assert key in expected and key not in seen
  seen.add(key);title,index,old,new=key;menus.add(title)
  assert old!=new and old in en and new in en and old in fr and new in fr
  a=en[title]['_choices'];b=fr[title]['_choices'];assert len(a)==len(b) and 0<=index<len(a)
  assert a[index]['_link']==old and b[index]['_link']==new
  assert emote(a[index]['_text'])==emote(b[index]['_text'])
 assert seen==expected
 validate_source_graph_alignment(name,english,french)
 for title in menus:
  corrected=deepcopy(en[title]['_choices'])
  for fix in fixes:
   if fix['passage']==title:corrected[fix['choice_index']]['_link']=fix['french_target']
  assert [{k:v for k,v in c.items() if k!='_text'} for c in corrected]==[{k:v for k,v in c.items() if k!='_text'} for c in fr[title]['_choices']],(name,title,'Other links/indices/ending flags must not move')

@lru_cache(maxsize=8)
def _original_french(relative_path,mtime_ns,size):
 data=read_json(ROOT/relative_path);dialog=next(x['dialog'] for x in data['dialogs'] if x['lang']==0)
 digest=hashlib.sha256(json.dumps(dialog,ensure_ascii=False,sort_keys=True).encode('utf-8')).hexdigest()
 return dict(name=data['m_Name'],kind='compiled',source_sha256=digest,passages=dialog['_passages'])

def apply_choice_target_restorations(source,reviewed,passage):
 name=source['name'];review=reviewed.get('french_review',{});fixes=review.get('choice_target_restorations',[])
 if name not in CASES and not fixes:return passage
 path=ROOT/source['original_file'];stat=path.stat();french=_original_french(source['original_file'],stat.st_mtime_ns,stat.st_size)
 validate_choice_target_restorations(name,review,source,french)
 result=deepcopy(passage)
 for fix in fixes:
  if fix['passage']==passage['_title']:
   index=fix['choice_index'];assert result['_choices'][index]['_link']==fix['english_target']
   result['_choices'][index]['_link']=fix['french_target']
 return result
