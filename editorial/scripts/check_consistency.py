"""Find registered contradictions; pending translations produce review candidates."""
from corpus import *

RULES=ROOT/'localization/consistency.json'

def agreement_applies(character,scene):
    """Generic role nouns may refer to different people in other scenes."""
    allowed=character.get('agreement_scenes')
    return allowed is None or scene in allowed

def preferred_object_present(obj,text):
    """Some roots overlap the speaker's name; require the declared noun forms."""
    if obj.get('preferred_pattern'):
        return re.search(obj['preferred_pattern'],text,re.I) is not None
    return obj['preferred_root'] in text.lower()

def forbidden_object_matches(obj,text):
    """Exact noun forms can exclude a knife without excluding its sheath."""
    return ([root for root in obj['forbidden_roots'] if root in text.lower()]
            +[pattern for pattern in obj.get('forbidden_patterns',[]) if re.search(pattern,text,re.I)])

def direct_agreement_conflicts(character,text):
    """Exclude only another person's explicitly named span, not the whole block."""
    excluded=[m.span() for pattern in character.get('direct_agreement_exclusions',[])
              for m in re.finditer(pattern,text,re.I)]
    for pattern in character.get('forbidden_direct_agreement',[]):
        if any(not any(a<=m.start()<b for a,b in excluded)
               for m in re.finditer(pattern,text,re.I)):
            yield pattern

def check_scene(source,review,rules):
    findings=[]
    units={u['passage']:u for u in review['passages']}
    for p in source['passages']:
        ru=units[p['_title']]
        text='\n'.join(ru['texts']+ru['choices'])
        for character in rules['characters']:
            if not agreement_applies(character,source['name']):continue
            for pattern in direct_agreement_conflicts(character,text):
                findings.append(dict(scene=source['name'],passage=p['_title'],
                    rule=character['key'],issue='contradictory_direct_gender_agreement',status='reviewed_conflict'))
        visible=[x for x in p['_lines'] if not command(x)]
        for obj in rules['objects']:
            if not source['name'].startswith(obj['scene_prefix']):continue
            for original,target in zip(visible,ru['texts']):
                if not re.search(r'\b'+re.escape(obj['source_term'])+r'\b',original,re.I):continue
                if not preferred_object_present(obj,target) or forbidden_object_matches(obj,target):
                    findings.append(dict(scene=source['name'],passage=p['_title'],rule=obj['key'],
                        issue='inconsistent_object_term',status='reviewed_conflict',english=original,russian=target))
    return findings

def main():
    from full_review import source_records
    rules=read_json(RULES)
    sources=source_records()
    q,f,_=russian_lookup(ROOT/'revision/data')
    findings=[];checked=0
    for name,source in sources.items():
        review_path=REVIEWED/f'{name}.json'
        if review_path.exists():
            findings+=check_scene(source,read_json(review_path),rules);checked+=1
            continue
        for p in source['passages']:
            block=get_block(q,f,name,p['_title'])
            if not block:continue
            text='\n'.join(block['lines']+[c['text'] for c in block['choices']])
            for character in rules['characters']:
                if not agreement_applies(character,name):continue
                for pattern in direct_agreement_conflicts(character,text):
                    findings.append(dict(scene=name,passage=p['_title'],rule=character['key'],
                        issue='possible_gender_conflict',status='pending_review_candidate',russian=text))
            for obj in rules['objects']:
                if not name.startswith(obj['scene_prefix']):continue
                english='\n'.join(x for x in p['_lines'] if not command(x))
                if not re.search(r'\b'+re.escape(obj['source_term'])+r'\b',english,re.I):continue
                bad=forbidden_object_matches(obj,text)
                if bad or not preferred_object_present(obj,text):
                    findings.append(dict(scene=name,passage=p['_title'],rule=obj['key'],
                        issue='possible_object_term_conflict',status='pending_review_candidate',
                        english=english,russian=text,matched_roots=bad))
    from extra_localization import consistency_findings
    findings += consistency_findings(rules)
    from passenger_call_defaults import consistency_findings as call_default_findings
    findings += call_default_findings()
    write_json(ROOT/'review/consistency_findings.json',findings)
    conflicts=sum(x['status']=='reviewed_conflict' for x in findings)
    print('Consistency: reviewed scenes checked',checked,'confirmed-rule conflicts',conflicts,
          'pending review candidates',len(findings)-conflicts)
    if conflicts:raise SystemExit(1)

if __name__=='__main__':main()
