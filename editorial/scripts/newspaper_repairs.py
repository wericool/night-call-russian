"""Only the enumerated original971 defects:29names,2dangling lists,9exhaustion exits."""
import copy,re
from corpus import ROOT,read_json

NAME='971_newspapers_eng'
EN_SHA='005d8a6231526561541655e6e7b8803b2f26dcb60053c6f2eb96667624367d6b'
FR_SHA='242fc7c2004e58cb276e57df0b4f6cb9b5b2d890aa9c0c36e2717a4c87d5ec4b'

def expected_record():
    renames=[];fallbacks=[]
    for owner,block,n in [('sand','Sand',10),('judge','Judge',9),('angel','Angel',10)]:
        for i in range(1,n+1):
            title=f'News-Killer-{block}-{i:02d}-'+('test' if owner=='sand' else 'Test')
            renames.append(dict(passage=title,source=f'{{ test_clue_{owner}_{i:02d} = 1 }}',target=f'{{ test_clue_{owner}{i:02d} = 1 }}'))
        hub='news-hub-killer-'+('sandman' if owner=='sand' else owner)
        for group in 'ABC':
            if group=='C':numbers=range(n,0,-1)
            elif group=='A':numbers=range(1 if owner=='sand' else 2,n+1,2)
            else:numbers=range(2 if owner=='sand' else 1,n+1,2)
            lines=[f'-> test_clue_{owner}{i:02d} = 0 ? News-Killer-{block}-{i:02d}-'+('test' if owner=='sand' else 'Test') for i in numbers]
            fallbacks.append(dict(passage=hub+group,source_lines=lines,target=hub+'C' if group!='C' else 'news-hub-clients'))
    randoms=[]
    for group,nums,missing in [('A',range(1,7),4),('D',range(17,22),17)]:
        links=[f'News-Misc-{i:02d}' for i in nums]
        randoms.append(dict(passage='news-hub-misc'+group,source='-> ?? '+' | '.join(links),target='-> ?? '+' | '.join(x for x in links if x!=f'News-Misc-{missing:02d}')))
    return dict(version=1,english_source_sha256=EN_SHA,french_source_sha256=FR_SHA,modifier_renames=renames,random_target_lists=randoms,exhaustion_fallbacks=fallbacks)

def blocks(script):
    result={};current=None
    for raw in script.splitlines():
        s=raw.strip()
        if s.startswith('==='):current=s[3:].strip();result[current]=[]
        elif current is not None and s and not s.startswith('%%'):result[current].append(s)
    return result

def validate_record(name,record,english,french):
    if name!=NAME:
        assert record is None,(name,'Newspaper repairs are scene-specific');return
    assert record==expected_record(),'Complete exact40-part newspaper repair record required; unknown/missing changes rejected'
    assert english['kind']==french['kind']=='script' and english['source_sha256']==EN_SHA and french['source_sha256']==FR_SHA
    originals=[blocks(read_json(ROOT/s['original_file'])['m_Script']) for s in [english,french]]
    for b in originals:
        titles=set(b)
        for fix in record['modifier_renames']:
            assert b[fix['passage']].count(fix['source'])==1 and fix['target'] not in b[fix['passage']]
        for fix in record['random_target_lists']:
            assert b[fix['passage']].count(fix['source'])==1
            before=fix['source'][6:].split(' | ');after=fix['target'][6:].split(' | ')
            assert set(before)-titles in [{'News-Misc-04'},{'News-Misc-17'}] and set(after)<=titles
            assert [x for x in before if x in titles]==after
        for fix in record['exhaustion_fallbacks']:
            assert b[fix['passage']]==fix['source_lines'] and fix['target'] in titles
            assert fix['target']=='news-hub-clients' or fix['target'].endswith('C')
    assert len(record['modifier_renames'])==29 and len(record['random_target_lists'])==2 and len(record['exhaustion_fallbacks'])==9

def apply_repairs(source,reviewed,script):
    record=reviewed.get('french_review',{}).get('newspaper_source_repairs')
    if source['name']!=NAME:
        assert record is None;return script
    assert record==expected_record() and source['source_sha256']==EN_SHA
    changes={x['passage']:x for x in record['modifier_renames']+record['random_target_lists']}
    exits={x['passage']:x['target'] for x in record['exhaustion_fallbacks']}
    out=[];current=None;seen=set();appended=set()
    def finish():
        if current in exits:
            out.append('\t\t-> '+exits[current]);out.append('');appended.add(current)
    for raw in script.splitlines():
        s=raw.strip()
        if s.startswith('==='):
            finish();current=s[3:].strip()
        fix=changes.get(current)
        if fix and s==fix['source']:
            assert current not in seen;seen.add(current)
            raw=raw[:len(raw)-len(raw.lstrip())]+fix['target']
        out.append(raw)
    finish()
    assert seen==set(changes) and appended==set(exits)
    return '\n'.join(out)+'\n'
