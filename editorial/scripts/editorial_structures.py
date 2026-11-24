"""Narrow, source-proven FR structural restorations in an otherwise EN carrier.

Reviewed base choices retain EN indices. Extra FR choices are explicit metadata,
so old icon-bearing choices cannot silently move to unrelated text.
"""
from copy import deepcopy
from corpus import ROOT, read_json, command, emote
import hashlib
import json

VINCENT_SILENCE = dict(name='008_vincent_01',passage='vincent-lostitem',
    original_file='original/DialogObjectScript/sharedassets0.assets/2139.json',
    english_sha256='1e7e2b70a037d20f7eeae66450ed73615feefbb3f68f62b63970aeb605585fe2',
    french_sha256='bc1b1622e319acb0fda7908adfc43da3c6f05b5322ecd23af44e105279cd7b44',
    english_insert_before=1,french_choice_index=1,russian=':silence: (Промолчать.)')


def validate_vincent_silence(name,review,english,french):
    if name!=VINCENT_SILENCE['name']:return
    allowed=VINCENT_SILENCE; fixes=review.get('additional_french_choices',[])
    assert len(fixes)==1,(name,'Original French silence answer must be retained')
    fix=fixes[0];assert set(fix)=={'passage','english_insert_before','french_choice_index','russian','reason'}
    for k in ['passage','english_insert_before','french_choice_index','russian']:assert fix[k]==allowed[k],(name,k)
    assert type(fix['english_insert_before']) is int and type(fix['french_choice_index']) is int
    assert isinstance(fix['reason'],str) and fix['reason'].strip() and not review.get('condition_flag_overrides')
    assert english['original_file']==french['original_file']==allowed['original_file']
    raw=read_json(ROOT/allowed['original_file']);assert raw['m_Name']==name
    src={x['lang']:x['dialog'] for x in raw['dialogs']}
    for language,record,key in [(1,english,'english_sha256'),(0,french,'french_sha256')]:
        digest=hashlib.sha256(json.dumps(src[language],ensure_ascii=False,sort_keys=True).encode('utf-8')).hexdigest()
        assert digest==record['source_sha256']==allowed[key]
        assert record['passages']==src[language]['_passages'],'Do not accept a forged source object with an old hash'
    assert src[1]['_variables']==src[0]['_variables']==[{'_name':'silence','_value':0}]
    en={p['_title']:p for p in english['passages']};fr={p['_title']:p for p in french['passages']}
    assert set(en)==set(fr) and len(en)==17
    for title,p in en.items():
        q=fr[title]
        assert p['_link']==q['_link'] and p['_variable_modifiers']==q['_variable_modifiers']
        assert [s for s in p['_lines'] if command(s)]==[s for s in q['_lines'] if command(s)]
        choices=deepcopy(q['_choices'])
        if title==allowed['passage']:
            assert len(p['_choices'])==1 and len(choices)==2
            extra=choices.pop(1);assert extra['_link']=='vincent-party' and extra['_ending']==0
            assert emote(extra['_text'])==':silence:'
        assert [{k:v for k,v in c.items() if k!='_text'} for c in p['_choices']]==[{k:v for k,v in c.items() if k!='_text'} for c in choices]
        assert [emote(c['_text']) for c in p['_choices']]==[emote(c['_text']) for c in choices]

# One demonstrable omission in the EN carrier. This is not a general licence to
# add narration or move source commands. Both immutable source hashes are pinned.
AMIRA_NOD = dict(
    name='206_amira_01', passage='amira-01-story02-game01-crime',
    english_sha256='7a15c5a69b3ba133ab3f8e5bb3f4c574674e7681d8c8eed1b1d10486a8b2430d',
    french_sha256='f224b3d919a04f822462256ad9272e0a36efd81ceb58f8c6ad81e93c12451fe4',
    english_insert_before=1, french_line_index=1,
    original_french='Elle hoche la tête.', russian='Она кивает.')


def validate_french_line_additions(name, review, english, french):
    additions=review.get('additional_french_lines',[])
    assert isinstance(additions,list)
    if name==AMIRA_NOD['name']:
        assert len(additions)==1,(name,'Original French narrator nod must not be silently omitted')
    if not additions:return
    assert len(additions)==1 and review.get('structural_alignment')
    fix=additions[0];allowed=AMIRA_NOD
    assert set(fix)=={'passage','english_insert_before','french_line_index','original_french','russian','reason'}
    assert name==allowed['name'] and english['kind']==french['kind']=='compiled'
    assert english['source_sha256']==allowed['english_sha256'] and french['source_sha256']==allowed['french_sha256']
    for key in ['passage','english_insert_before','french_line_index','original_french','russian']:
        assert fix[key]==allowed[key],(name,'Unproven French narrative insertion',key)
    assert type(fix['english_insert_before']) is int and type(fix['french_line_index']) is int
    assert isinstance(fix['reason'],str) and fix['reason'].strip()
    ep=next(p for p in english['passages'] if p['_title']==fix['passage'])
    fp=next(p for p in french['passages'] if p['_title']==fix['passage'])
    assert {k:v for k,v in ep.items() if k!='_lines'}=={k:v for k,v in fp.items() if k!='_lines'}
    assert not any(command(x) for x in ep['_lines']+fp['_lines'])
    assert len(ep['_lines'])==3 and len(fp['_lines'])==4
    assert fp['_lines'][1]==allowed['original_french']
    assert all(x.startswith('AMIRA :') for x in ep['_lines'])
    assert all(x.startswith('AMIRA :') for i,x in enumerate(fp['_lines']) if i!=1)


def validate_structural_overrides(name, review, english, french):
    from myriam_navigation import validate_default_links
    validate_default_links(name,review,english,french)
    from french_navigation import validate_choice_target_restorations
    validate_choice_target_restorations(name,review,english,french)
    validate_french_line_additions(name,review,english,french)
    validate_vincent_silence(name,review,english,french)
    additions=review.get('additional_french_choices',[])
    flags=review.get('condition_flag_overrides',[])
    assert isinstance(additions,list) and isinstance(flags,list)
    if not additions and not flags:return
    assert english['kind']=='compiled' and review.get('structural_alignment')
    ep={p['_title']:p for p in english['passages']};fp={p['_title']:p for p in french['passages']}
    seen=set()
    for fix in flags:
        title=fix['passage'];old=fix['source_flag'];new=fix['french_flag']
        assert title not in seen and fix.get('reason') and old and new and old!=new
        seen.add(title);a=ep[title]['_link'];b=fp[title]['_link']
        assert a.count(old)==1 and a.replace(old,new)==b and a!=b,(name,'Only one corroborated flag spelling may change')
        path=(ROOT/fix['corroborating_file']).resolve()
        assert path.is_relative_to((ROOT/'original/DialogObjectScript').resolve())
        prior=read_json(path)
        for lang in [0,1]:
            dialog=next(x['dialog'] for x in prior['dialogs'] if x['lang']==lang)
            p=next(p for p in dialog['_passages'] if p['_title']==fix['corroborating_passage'])
            assert new+'=1' in p['_variable_modifiers'],(name,'Prior encounter must set the actual flag in both sources')
            assert not any(old in m for p in dialog['_passages'] for m in p['_variable_modifiers'])
    grouped={};seen=set()
    for fix in additions:
        title=fix['passage'];i=fix['english_insert_before'];j=fix['french_choice_index']
        assert type(i) is int and type(j) is int and 0<=i<=len(ep[title]['_choices']) and 0<=j<len(fp[title]['_choices'])
        assert (title,i) not in seen and fix.get('reason') and fix.get('russian')
        seen.add((title,i));grouped.setdefault(title,[]).append(fix)
        choice=fp[title]['_choices'][j]
        assert choice['_link'] in ep and choice['_link'] not in [c['_link'] for c in ep[title]['_choices']]
        assert not command(fix['russian']) and emote(fix['russian'])==emote(choice['_text'])
        assert all(set(c)==set(choice) for c in ep[title]['_choices'])
    for title,fixes in grouped.items():
        base=deepcopy(ep[title]['_choices']);target=fp[title]['_choices']
        for offset,fix in enumerate(sorted(fixes,key=lambda f:f['english_insert_before'])):
            assert fix['french_choice_index']==fix['english_insert_before']+offset
            base.insert(fix['french_choice_index'],deepcopy(target[fix['french_choice_index']]))
        assert len(base)==len(target)
        # Existing links and ending flags stay intact; inserted fields are exact FR.
        assert [{k:v for k,v in c.items() if k!='_text'} for c in base]==[{k:v for k,v in c.items() if k!='_text'} for c in target]


def apply_compiled_review(source, reviewed, passage):
    """Return the exact translated compiled passage, leaving immutable inputs alone."""
    assert source['kind']=='compiled'
    result=deepcopy(passage);title=passage['_title']
    unit=next(p for p in reviewed['passages'] if p['passage']==title)
    from french_command_alignment import source_lines as aligned_command_lines
    it=iter(unit['texts']);result['_lines']=[x if command(x) else next(it) for x in aligned_command_lines(source['name'],reviewed.get('french_review',{}),passage)]
    assert len(result['_choices'])==len(unit['choices'])
    for c,text in zip(result['_choices'],unit['choices']):c['_text']=text
    review=reviewed.get('french_review',{})
    if source['name']==VINCENT_SILENCE['name'] and title==VINCENT_SILENCE['passage']:
        from french_sources import french_source_records
        validate_vincent_silence(source['name'],review,source,french_source_records()[source['name']])
    for fix in review.get('condition_flag_overrides',[]):
        if fix['passage']==title:
            assert result['_link'].count(fix['source_flag'])==1
            result['_link']=result['_link'].replace(fix['source_flag'],fix['french_flag'])
    additions=[f for f in review.get('additional_french_choices',[]) if f['passage']==title]
    if additions:
        data=read_json(ROOT/source['original_file'])
        french=next(x['dialog'] for x in data['dialogs'] if x['lang']==0)
        fp=next(p for p in french['_passages'] if p['_title']==title)
        for fix in sorted(additions,key=lambda f:f['french_choice_index']):
            c=deepcopy(fp['_choices'][fix['french_choice_index']]);c['_text']=fix['russian']
            result['_choices'].insert(fix['french_choice_index'],c)
    # Validation has proved a command-free, three-speech-line EN paragraph and
    # the fourth FR narrator line. No existing line is merged or reattributed.
    line_additions=reviewed.get('french_review',{}).get('additional_french_lines',[])
    if (source['name']==AMIRA_NOD['name'] and title==AMIRA_NOD['passage']) or any(f['passage']==title for f in line_additions):
        from french_sources import french_source_records
        validate_french_line_additions(source['name'],reviewed['french_review'],source,french_source_records()[source['name']])
    for fix in line_additions:
        if fix['passage']==title:
            assert not any(command(x) for x in result['_lines'])
            result['_lines'].insert(fix['english_insert_before'],fix['russian'])
    from french_navigation import apply_choice_target_restorations
    from source_defect_fixes import corrected_link
    result=apply_choice_target_restorations(source,reviewed,result)
    from myriam_navigation import apply_default_links
    result=apply_default_links(source,reviewed,result)
    result['_link']=corrected_link(reviewed,title,result['_link'])
    from skaterz_french_structure import apply_link as restore_skaterz_link
    result['_link']=restore_skaterz_link(source['name'],review,title,result['_link'])
    from vero_french_choice_order import apply_order as restore_vero_order
    return restore_vero_order(source,reviewed,result)
