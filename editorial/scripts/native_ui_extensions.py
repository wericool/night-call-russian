"""Two source-backed missing locale entries; legacy aliases remain separate."""
import copy,hashlib
from pathlib import Path
from corpus import ROOT,read_json

POLICY=ROOT/'localization/native_ui_extensions.json'
CLUE='clue_judge_journal09_01'
TITLE='Документы о первых трёх жертвах утекли из полиции два года назад'
SOURCE='Прочитано в газете…'
EVIDENCE='review/newspaper_missing_clue_ui_evidence.json'
EVIDENCE_SHA='434fceacc8d4063a6045461c1053f2cc0d3f99fc54d38c182118d8cc8e80266d'
EN_TITLE='First 3 victims files = part of police leak 2 years ago'

def expected_policy():
    return dict(version=1,target_slot='ENG',clue=CLUE,evidence=EVIDENCE,evidence_sha256=EVIDENCE_SHA,
        keys=[dict(key=CLUE+'.title',russian=TITLE,english=EN_TITLE,
            french_evidence='Il y a deux ans, de nombreux documents confidentiels de la police avaient été diffusés par accident.\nNotamment des documents liés à Ronan Besson, Charles Bougrain et Simon Gorodischer, les trois premières victimes du Juge.',
            source_kind='existing_clue_and_french_article',source='original/TextAsset/sharedassets0.assets/799.json',passage='News-Killer-Judge-09'),
              dict(key=CLUE+'.source',russian=SOURCE,english='Read in a newspaper…',french_evidence='Lu dans un journal…',
            source_kind='existing_clue_and_shared_french_newspaper_source_label',source='original/LocalizationData/sharedassets0.assets/2097.json',source_key='clue_judge_journal08_01.source')])

def validate_policy(policy):
    assert policy==expected_policy(),'Only the two exact source-backed Judge09 locale entries are accepted'
    assert hashlib.sha256((ROOT/EVIDENCE).read_bytes()).hexdigest()==EVIDENCE_SHA
    e=read_json(ROOT/EVIDENCE);assert e['passed'] and e['existing_clue']['clue']['id']==CLUE
    path=ROOT/e['existing_clue']['file'];assert hashlib.sha256(path.read_bytes()).hexdigest()==e['existing_clue']['file_sha256']
    clue=read_json(path)['clues'][111];assert clue==e['existing_clue']['clue'] and clue['title']==EN_TITLE and clue['source_name']=='Read in a newspaper…'
    same_title=[c['id'] for row in read_json(ROOT/'original/inventory.json') if row['cls']=='InvestigationScript' for c in read_json(ROOT/row['file'])['clues'] if c.get('title')==EN_TITLE]
    assert same_title==['clue_judge_radionews01_01',CLUE],'Shared raw title is known: add id-qualified keys without changing the legacy raw-title fallback'
    script=read_json(ROOT/'original/TextAsset/sharedassets0.assets/799.json')['m_Script']
    assert all(line in script for line in policy['keys'][0]['french_evidence'].splitlines())
    fr={x['_key']:x['_value'] for x in read_json(ROOT/'original/LocalizationData/sharedassets0.assets/2097.json')['_items']}
    en={x['_key']:x['_value'] for x in read_json(ROOT/'original/LocalizationData/sharedassets0.assets/2096.json')['_items']}
    assert fr['clue_judge_journal08_01.source']==policy['keys'][1]['french_evidence']
    assert all(x['key'] not in en and x['key'] not in fr for x in policy['keys'])
    return copy.deepcopy(policy['keys'])

def reviewed_extensions(ui=None):
    if not POLICY.exists():return []
    rows=validate_policy(read_json(POLICY))
    if ui is not None:assert all(ui[x['key']]==x['russian'] for x in rows),'Native extension values do not match reviewed source-backed policy'
    return rows

def translated_items(original_items,ui):
    assert len(original_items)==921 and all(set(x)=={'_key','_value'} for x in original_items)
    rows=reviewed_extensions(ui);keys={x['_key'] for x in original_items}
    assert len(keys)==921 and all(x['key'].upper() not in {k.upper() for k in keys} for x in rows)
    result=copy.deepcopy(original_items)
    for x in result:x['_value']=ui[x['_key']]
    result.extend(dict(_key=x['key'],_value=x['russian']) for x in rows)
    return result
