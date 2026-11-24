"""Inventory the new full FR-priority pass without relabelling legacy reviews."""
import re
from full_review import *
from french_sources import french_source_records,validate_french_review


def main():
    english=source_records();french=french_source_records();progress=read_json(DEST/'progress.json')
    anomalies=read_json(ROOT/'review/source_anomalies.json');rows=[]
    for name,en in english.items():
        fr=french.get(name);assert fr is not None,('Missing bundled French source',name)
        reviewed_path=REVIEWED/(name+'.json');review=read_json(reviewed_path) if reviewed_path.exists() else None
        done=False
        if review and review.get('french_review'):
            validate_french_review(name,review['french_review'],en,french_source=fr);done=True
        et={p['_title']:p for p in en['passages']};ft={p['_title']:p for p in fr['passages']}
        counts=[];navigation=[]
        for title in sorted(set(et)&set(ft)):
            a=et[title];b=ft[title]
            ec=sum(not command(x) for x in a['_lines']);fc=sum(not command(x) for x in b['_lines'])
            if ec!=fc or len(a['_choices'])!=len(b['_choices']):
                counts.append(dict(passage=title,english_visible=ec,french_visible=fc,english_choices=len(a['_choices']),french_choices=len(b['_choices'])))
            for index,(ec,fc) in enumerate(zip(a['_choices'],b['_choices'])):
                if ec['_link']!=fc['_link']:
                    navigation.append(dict(passage=title,choice_index=index,english_target=ec['_link'],french_target=fc['_link'],english_target_exists=ec['_link'] in et,french_target_exists=fc['_link'] in ft))
        known=[]
        for a in anomalies:
            if name in str(a.get('scene','')) or name in str(a.get('scenes','')):
                known.append(dict(id=a['id'],language_comparison_hint=bool(re.search(r'French|français|француз|\bFR\b|EN.*FR|FR.*EN',str(a),re.I)),status='historical_decision_requires_FR_priority_review'))
        rows.append(dict(scene=name,kind=en['kind'],status='french_priority_reviewed' if done else 'pending',
            previous_editorial_status='contextually_reviewed' if review else 'pending',
            english_source_sha256=en['source_sha256'],french_source_sha256=fr['source_sha256'],
            french_original_file=fr['original_file'],french_passages=len(ft),english_passages=len(et),
            french_only_passages=sorted(set(ft)-set(et)),english_only_passages=sorted(set(et)-set(ft)),
            count_differences=counts,choice_navigation_differences=navigation,known_anomalies=known))
    enui=original_ui();frui={x['_key']:x['_value'] for x in read_json(ROOT/'original/LocalizationData/sharedassets0.assets/2097.json')['_items']}
    assert set(enui)<=set(frui)
    ui_records={}
    for path in (DEST/'contexts').glob('*_ui.json'):
        for item in read_json(path).get('keys',[]):
            if isinstance(item,dict) and item.get('french_priority_reviewed') is True:
                assert item['french']==frui[item['key']]
                ui_records[item['key']]=path.relative_to(ROOT).as_posix()
    data=dict(primary_language='fr',secondary_language='en',policy='localization/source_priority.json',
        scope='New semantic source-priority pass. Earlier contextual reviews/structural checks remain evidence for their recorded scope;they do not automatically prove full FR-priority acceptance.',
        total=len(rows),reviewed=sum(x['status']=='french_priority_reviewed' for x in rows),
        pending=sum(x['status']=='pending' for x in rows),previously_contextually_reviewed=progress['reviewed'],
        french_source_coverage=len(rows),scenes=rows,ui_total=len(enui),ui_reviewed=len(ui_records),
        ui_pending=[key for key in enui if key not in ui_records],ui_reviewed_records=ui_records,
        boundaries=['All original French passages,choices,call metadata and linkedUI need manual checking.',
            'Structural/title/count differences are alignment tasks,not automatic semantic error diagnoses.',
            'Known anomalies are historical retrieval hints,not a complete list of differences.',
            'No automatic story/name/gender/command replacements;technical native carrier is unchanged until explicit alignment decisions.'])
    write_json(DEST/'french_priority_audit.json',data)
    from native_ui_extensions import reviewed_extensions
    extra=reviewed_extensions(read_json(UI_KEYS))
    data['native_ui_extension_total']=len(extra);data['native_ui_extension_reviewed']=len(extra)
    data['native_ui_extension_keys']=[x['key'] for x in extra]
    write_json(DEST/'french_priority_audit.json',data)
    print('Original French coverage:',len(rows),'of',len(english),'scenes. New FR-priority pass:',data['reviewed'],'reviewed;',data['pending'],'pending. Earlier contextual reviews:',progress['reviewed'])
    print('FR/EN alignment:',sum(bool(x['french_only_passages'] or x['english_only_passages'] or x['count_differences']) for x in rows),'scenes have title/count differences;this is not a count of meaning errors.')
    print('UI FR-priority:',data['ui_reviewed'],'of',data['ui_total'],'explicitly accepted. No legacy statuses silently upgraded.')
    print('Choice navigation differences:',sum(len(x['choice_navigation_differences']) for x in rows),'across',sum(bool(x['choice_navigation_differences']) for x in rows),'scenes;manual alignment required.')

if __name__=='__main__':main()
