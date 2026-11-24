"""Contextual editorial layer. A complete scene must explicitly cover every source block."""
from corpus import *
import hashlib
from dialog_metadata import passenger_calls,reviewed_calls,apply_calls
from editorial_structures import apply_compiled_review

DEST=ROOT/'localization'

def source_records():
    records={}
    for row in read_json(ROOT/'original/inventory.json'):
        d=read_json(ROOT/row['file'])
        if row['cls']=='DialogObjectScript':
            eng=next(x['dialog'] for x in d['dialogs'] if x['lang']==1)
            raw=json.dumps(eng,ensure_ascii=False,sort_keys=True).encode('utf-8')
            records[d['m_Name']]=dict(name=d['m_Name'],kind='compiled',asset=row['asset'],path_id=row['path_id'],
                passages=eng['_passages'],user_datas=eng['_user_datas'],source_sha256=hashlib.sha256(raw).hexdigest(),original_file=row['file'])
        elif row['cls']=='TextAsset' and d['m_Name'].endswith('_eng'):
            path=DEST/'sources'/f'{d["m_Name"]}.txt'
            cache_source_text(path,d['m_Script'])
            blocks,_=parse_text_content(d['m_Script'])
            records[d['m_Name']]=dict(name=d['m_Name'],kind='script',asset=row['asset'],path_id=row['path_id'],
                script=d['m_Script'],passages=[dict(_title=b['title'],_lines=b['lines'],_choices=[dict(_text=c['text'],_link=c['link']) for c in b['choices']]) for b in blocks.values()],
                source_sha256=hashlib.sha256(d['m_Script'].encode('utf-8')).hexdigest(),original_file=row['file'])
    return records

def save_scene(name,texts,choices,context,decisions,calls=None,french_review=None):
    source=source_records()[name]
    priority_path=DEST/'source_priority.json'
    if priority_path.exists() and read_json(priority_path).get('primary_language')=='fr':
        from french_sources import validate_french_review
        french_review=validate_french_review(name,french_review,source)
    expected={p['_title'] for p in source['passages']}
    assert set(texts)==expected,(name,'missing passages',sorted(expected-set(texts)),'extra',sorted(set(texts)-expected))
    units=[]
    for p in source['passages']:
        title=p['_title']
        visible=[x for x in p['_lines'] if not command(x)]
        target=texts[title].splitlines() if isinstance(texts[title],str) else texts[title]
        answers=choices.get(title,[])
        assert len(visible)==len(target),(name,title,len(visible),len(target))
        assert len(p['_choices'])==len(answers),(name,title,'choice count',len(p['_choices']),len(answers))
        from source_defect_fixes import expected_marker
        for ci,(a,b) in enumerate(zip(p['_choices'],answers)):
            assert expected_marker(french_review,title,ci,emote(a['_text']))==emote(b),(name,title,'choice icon',a['_text'],b)
        assert all(not command(x) for x in target)
        units.append(dict(passage=title,texts=target,choices=answers))
    result=dict(name=name,kind=source['kind'],status='contextually_reviewed',source_sha256=source['source_sha256'],
                context=context,decisions=decisions,passages=units,
                passenger_calls=reviewed_calls(name,source.get('user_datas',[]),calls))
    if french_review is not None:result['french_review']=french_review
    rules_path=DEST/'consistency.json'
    if rules_path.exists():
        from check_consistency import check_scene
        conflicts=check_scene(source,result,read_json(rules_path))
        assert not conflicts,(name,'registered consistency conflict',conflicts)
    write_json(REVIEWED/f'{name}.json',result)
    return result

def reviewed_choice_target(source,reviewed,title,index):
    original=next(p for p in source['passages'] if p['_title']==title)['_choices'][index]['_link']
    fixes=reviewed.get('french_review',{}).get('choice_link_overrides',[])
    for fix in fixes:
        if fix['passage']==title and fix['choice_index']==index:
            assert fix['source_link']==original,(source['name'],title,'Choice alignment source changed')
            return fix['french_link']
    return original


def render(source,reviewed):
    units={u['passage']:u for u in reviewed['passages']}
    if source['kind']=='compiled':
        parts=[]
        for p in source['passages']:
            result=apply_compiled_review(source,reviewed,p)
            parts+=['=== '+p['_title'],'',*result['_lines'],*['* '+c['_text']+' -> '+c['_link'] for c in result['_choices']],'','']
        return '\n'.join(parts)
    current=None;line_index=0;choice_index=0;output=[]
    for raw in source['script'].splitlines():
        s=raw.strip()
        if s.startswith('==='):
            current=s[3:].strip();line_index=0;choice_index=0
            output.append(raw);continue
        fallback=next((fix for fix in reviewed.get('french_review',{}).get('passage_link_overrides',[]) if fix['passage']==current and s=='-> '+fix['source_link']),None)
        if fallback:
            output.append(raw[:len(raw)-len(raw.lstrip())]+'-> '+fallback['target_link']);continue
        if current is None or not s or s.startswith(('%%','$$','->')) or s.startswith('{') and s.endswith('}') or ';;' in s:
            output.append(raw);continue
        if s.startswith('*'):
            _,sep,link=s[1:].rpartition('->')
            assert sep,(source['name'],current,s)
            target=reviewed_choice_target(source,reviewed,current,choice_index)
            assert link.strip()==next(p for p in source['passages'] if p['_title']==current)['_choices'][choice_index]['_link']
            output.append('* '+units[current]['choices'][choice_index]+' -> '+target);choice_index+=1
        else:
            output.append(units[current]['texts'][line_index]);line_index+=1
    from newspaper_repairs import apply_repairs
    from sleep_french_navigation import apply_repair as apply_sleep_navigation
    from gasstation_french_state import apply_repair as apply_station_state
    from sleep_pierrette_condition_repair import apply_repair as repair_sleep_pierrette
    return repair_sleep_pierrette(source,reviewed,apply_station_state(source,reviewed,apply_sleep_navigation(source,reviewed,apply_repairs(source,reviewed,'\n'.join(output)+'\n'))))

def main():
    sources=source_records()
    from french_sources import french_source_records,validate_french_review
    french_sources=french_source_records() # One immutable original snapshot for this batch.
    reviews={p.stem:read_json(p) for p in (REVIEWED).glob('*.json')}
    q,f,_=russian_lookup(ROOT/'upstream/data')
    changes=[];progress=[];call_inventory=[]
    for name,source in sources.items():
        r=reviews.get(name)
        stats=dict(name=name,kind=source['kind'],passages=len(source['passages']),
                   visible_lines=sum(not command(s) for p in source['passages'] for s in p['_lines']),
                   choices=sum(len(p['_choices']) for p in source['passages']),
                   passenger_calls=len(passenger_calls(source.get('user_datas',[]))),status='pending')
        if r:
            assert r['source_sha256']==source['source_sha256'],(name,'source changed since review')
            if r.get('french_review') is not None:
                validate_french_review(name,r['french_review'],source,french_source=french_sources[name])
            expected=passenger_calls(source.get('user_datas',[]))
            calls=r.get('passenger_calls',[])
            assert [{k:c[k] for k in ['index','english','prefix','original']} for c in calls]==expected,(name,'call preview coverage')
            apply_calls(source.get('user_datas',[]),calls)
            output=render(source,r)
            raw_name=name[:-4] if name.endswith('_eng') else name
            dest=ROOT/'revision/data/Russian_Texts'/f'{raw_name}_rus.txt'
            dest.write_text(output,encoding='utf-8')
            if source['kind']=='script':
                (DEST/'native_scripts').mkdir(exist_ok=True,parents=True)
                (DEST/'native_scripts'/f'{name}.txt').write_text(output,encoding='utf-8')
            blocks,_=parse_text(dest)
            assert len(blocks)==len(source['passages'])
            for p in source['passages']:
                b=blocks[p['_title']]
                expected=apply_compiled_review(source,r,p) if source['kind']=='compiled' else p
                assert [s for s in b['lines'] if command(s)]==[s for s in expected['_lines'] if command(s)]
                assert len(b['lines'])==len(expected['_lines']),(name,p['_title'],'rendered line count')
                targets=[c['_link'] for c in expected['_choices']] if source['kind']=='compiled' else [reviewed_choice_target(source,r,p['_title'],i) for i in range(len(p['_choices']))]
                assert [c['link'] for c in b['choices']]==targets,(name,p['_title'],'rendered choice targets')
                from source_defect_fixes import expected_marker
                markers=[emote(c['_text']) if source['kind']=='compiled' else expected_marker(r.get('french_review'),p['_title'],i,emote(c['_text'])) for i,c in enumerate(expected['_choices'])]
                assert [emote(c['text']) for c in b['choices']]==markers,(name,p['_title'],'rendered choice icons')
                before=get_block(q,f,name,p['_title'])
                if before and before['lines']==b['lines'] and [c['text'] for c in before['choices']]==[c['text'] for c in b['choices']]:
                    continue
                if not before and not b['lines'] and not b['choices']:
                    continue
                changes.append(dict(category='dialogue',phase='full_context_review',object=name,passage=p['_title'],
                    english=p['_lines'],before=before['lines'] if before else [],after=b['lines'],
                    english_choices=[c['_text'] for c in p['_choices']],before_choices=[c['text'] for c in before['choices']] if before else [],
                    after_choices=[c['text'] for c in b['choices']],reason='Полная контекстная редактура сцены. '+r['context']))
            stats['status']='contextually_reviewed'
            for c in calls:
                changes.append(dict(category='passenger_call',phase='full_context_review',object=name,
                    passage='@passengercall['+str(c['index'])+']',english=c['english'],before=c['english'],
                    after=c['russian'],reason='Короткая фраза заказа такси хранится отдельно в данных диалога. Было — исходная строка ресурса; её отображение старым модом отдельно не подтверждено. '+r['context']))
        progress.append(stats)
        call_inventory.extend(dict(c,scene=name,status=stats['status']) for c in passenger_calls(source.get('user_datas',[])))
    write_json(DEST/'progress.json',dict(scenes=progress,total=len(sources),reviewed=len(reviews),pending=len(sources)-len(reviews),
        reviewed_additional_french_lines=sum(len(r.get('french_review',{}).get('additional_french_lines',[])) for r in reviews.values()),
        reviewed_additional_french_choices=sum(len(r.get('french_review',{}).get('additional_french_choices',[])) for r in reviews.values()),
        reviewed_passages=sum(s['passages'] for s in progress if s['status']=='contextually_reviewed'),
        reviewed_visible_lines=sum(s['visible_lines'] for s in progress if s['status']=='contextually_reviewed'),
        reviewed_choices=sum(s['choices'] for s in progress if s['status']=='contextually_reviewed'),
        total_passenger_calls=sum(s['passenger_calls'] for s in progress),
        reviewed_passenger_calls=sum(s['passenger_calls'] for s in progress if s['status']=='contextually_reviewed'),
        reviewed_technical_call_markers=sum(bool(c.get('technical_marker')) for r in reviews.values() for c in r.get('passenger_calls',[])),
        reviewed_translated_passenger_calls=sum(not c.get('technical_marker',False) for r in reviews.values() for c in r.get('passenger_calls',[]))))
    write_json(ROOT/'review/full_review_changes.json',changes)
    write_json(DEST/'passenger_call_inventory.json',call_inventory)
    print('Contextually reviewed complete scenes:',len(reviews),'/',len(sources))
    print('Passages:',sum(x['passages'] for x in progress),'Visible lines:',sum(x['visible_lines'] for x in progress),'Choices:',sum(x['choices'] for x in progress))

if __name__=='__main__':main()
