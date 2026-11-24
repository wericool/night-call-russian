"""Original FR records; semantic priority is separate from native carrier IDs."""
import hashlib
import re
from corpus import ROOT,read_json,parse_text_content,cache_source_text


def french_source_records():
    result={}
    for row in read_json(ROOT/'original/inventory.json'):
        if row['cls'] not in ['DialogObjectScript','TextAsset']:continue
        data=read_json(ROOT/row['file'])
        if row['cls']=='DialogObjectScript':
            french=next((x['dialog'] for x in data['dialogs'] if x['lang']==0),None)
            if french is None:continue
            raw=__import__('json').dumps(french,ensure_ascii=False,sort_keys=True).encode('utf-8')
            result[data['m_Name']]=dict(name=data['m_Name'],kind='compiled',language='fr',
                source_sha256=hashlib.sha256(raw).hexdigest(),original_file=row['file'],
                passages=french['_passages'],user_datas=french.get('_user_datas',[]))
        elif data['m_Name'].endswith('_fra'):
            name=data['m_Name'][:-4]+'_eng';script=data['m_Script']
            # Resumable exact snapshots of bundled originals; never alter game files.
            path=ROOT/'localization/sources_french'/(data['m_Name']+'.txt')
            cache_source_text(path,script)
            blocks,_=parse_text_content(script)
            result[name]=dict(name=name,kind='script',language='fr',original_name=data['m_Name'],
                source_sha256=hashlib.sha256(script.encode('utf-8')).hexdigest(),original_file=row['file'],
                passages=[dict(_title=b['title'],_lines=b['lines'],_choices=[dict(_text=c['text'],_link=c['link']) for c in b['choices']]) for b in blocks.values()],user_datas=[])
    return result


def validate_french_review(name,review,english_source,*,french_source=None):
    assert isinstance(review,dict) and review.get('complete') is True,(name,'Explicit complete french_review required by current source priority')
    source=french_source if french_source is not None else french_source_records().get(name)
    assert source is not None and source['name']==name,(name,'Original French source missing/mismatched')
    assert review.get('source_sha256')==source['source_sha256'],(name,'French source hash changed/missing')
    titles={p['_title'] for p in source['passages']}
    assert set(review.get('checked_french_passages',[]))==titles,(name,'Full original French passage coverage required')
    assert isinstance(review.get('decisions'),list) and review['decisions'],(name,'Explicit French semantic/alignment decisions required')
    assert review.get('passenger_calls_checked') is True,(name,'French passenger-call metadata must be checked too')
    english_titles={p['_title'] for p in english_source['passages']}
    if english_titles!=titles:
        assert review.get('structural_alignment'),(name,'Different FR/EN blocks require an explicit alignment record')
    fixes=review.get('choice_link_overrides',[])
    assert isinstance(fixes,list),(name,'Choice alignment must be an explicit list')
    seen=set()
    for fix in fixes:
        assert review.get('structural_alignment') and fix.get('reason'),(name,'Choice alignment needs evidence and reason')
        title=fix['passage'];index=fix['choice_index'];pair=(title,index)
        assert isinstance(index,int) and index>=0 and pair not in seen,(name,'Duplicate/invalid choice alignment')
        seen.add(pair)
        ep=next(p for p in english_source['passages'] if p['_title']==title)
        fp=next(p for p in source['passages'] if p['_title']==title)
        assert index<len(ep['_choices']) and index<len(fp['_choices']),(name,'Choice alignment index out of range')
        assert ep['_choices'][index]['_link']==fix['source_link'],(name,'Choice source link changed')
        assert fp['_choices'][index]['_link']==fix['french_link'],(name,'Choice target is not the original French target')
        assert fix['french_link'] in titles,(name,'French choice target missing')
        assert fix['source_link'] not in english_titles,(name,'This exception is limited to a demonstrably missing English target')
    fallback_fixes=review.get('passage_link_overrides',[])
    assert isinstance(fallback_fixes,list),(name,'Passage fixes must be explicit')
    seen=set()
    for fix in fallback_fixes:
        assert english_source['kind']=='script' and review.get('structural_alignment') and fix.get('reason'),(name,'Source defect requires script and explicit evidence')
        title=fix['passage'];assert title not in seen;seen.add(title)
        assert fix['source_link'] not in english_titles|titles,(name,'Original fallback must be demonstrably absent in both sources')
        assert fix['target_link'] in english_titles&titles,(name,'Corrected fallback must exist in both originals')
        original_fr=read_json(ROOT/source['original_file'])['m_Script']
        for script in [english_source['script'],original_fr]:
            current=None;lines=[];parallel=[]
            for line in script.splitlines():
                value=line.strip()
                if value.startswith('==='):current=value[3:].strip()
                elif current==title:lines.append(value)
                elif current==fix['corroborating_passage']:parallel.append(value)
            assert lines.count('-> '+fix['source_link'])==1,(name,'Exact original fallback absent/ambiguous')
            assert '-> '+fix['target_link'] not in lines,(name,'No need to rewrite an existing correct fallback')
            assert fix['corroborating_passage']!=title and parallel.count('-> '+fix['target_link'])==1,(name,'Existing parallel fallback does not corroborate target')
            condition=lambda values:[re.sub(r'\s+','',v.split('?',1)[0]) for v in values if v.startswith('->') and '?' in v]
            assert condition(lines) and condition(lines)==condition(parallel),(name,'Parallel condition must match original predicate')
    from editorial_structures import validate_structural_overrides
    validate_structural_overrides(name,review,english_source,source)
    from source_defect_fixes import validate_source_defects
    validate_source_defects(name,review,english_source,source)
    from newspaper_repairs import validate_record
    validate_record(name,review.get('newspaper_source_repairs'),english_source,source)
    from sleep_french_navigation import validate as validate_sleep_navigation
    validate_sleep_navigation(name,review,english_source,source)
    from sleep_pierrette_condition_repair import validate as validate_sleep_pierrette
    validate_sleep_pierrette(name,review,english_source,source)
    from gasstation_french_state import validate as validate_station_state
    validate_station_state(name,review,english_source,source)
    from french_script_choice_markers import validate as validate_script_markers
    validate_script_markers(name,review,english_source,source)
    from french_choice_markers import validate as validate_french_markers
    validate_french_markers(name,review,english_source,source)
    from french_command_alignment import validate as validate_french_commands
    validate_french_commands(name,review,english_source,source)
    from francois_sound_repairs import validate as validate_francois_sound
    validate_francois_sound(name,review,english_source,source)
    from skaterz_french_structure import validate as validate_skaterz_structure
    validate_skaterz_structure(name,review,english_source,source)
    from vero_french_choice_order import validate as validate_vero_order
    validate_vero_order(name,review,english_source,source)
    from annabelle_french_door_sfx import validate as validate_annabelle_door
    validate_annabelle_door(name,review,english_source,source)
    from tps_mathieu_french_technical import validate as validate_tps_mathieu
    validate_tps_mathieu(name,review,english_source,source)
    from mathieu_followup_markers import validate as validate_mathieu_followup
    validate_mathieu_followup(name,review,english_source,source)
    from myriam_french_commands import validate as validate_myriam_commands
    validate_myriam_commands(name,review,english_source,source)
    from esmeralda_parent_alignment import validate as validate_esmeralda_parent_timing
    validate_esmeralda_parent_timing(name,review,english_source,source)
    from chiara_radio_cutscene_repair import validate as validate_chiara_radio
    validate_chiara_radio(name,review,english_source,source)
    from vero_travel_command_repair import validate as validate_vero_travel
    validate_vero_travel(name,review,english_source,source)
    return dict(review,language='fr',original_file=source['original_file'])
