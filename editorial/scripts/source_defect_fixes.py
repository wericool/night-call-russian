"""Two narrow, user-approved (17.10.2026) repairs of proven defects shared by BOTH original languages.

Each repair is pinned to one scene/passage and exact source values. This is not a general
licence to change links or icons; anything else still goes through the older guards.
"""
from corpus import ROOT, read_json, emote

HOTEL = dict(name='010_camilleantoine_01', passage='ca-drive2',
    source_link='hotel=1?ca-hotel-perfect;;hotel=5?ca-hotel-average;;ca-hotel-average',
    corrected_link='hotel=5?ca-hotel-perfect;;hotel=1?ca-hotel-average;;ca-hotel-average')
ANGRY = dict(name='015_alicia_01', passage='alicia-OK-intro', choice_index=0,
    source_marker=':angry:', corrected_marker=':anger:')


def _passage(source, title):
    return next(p for p in source['passages'] if p['_title'].lower() == title.lower())


def validate_source_defects(name, review, english, french):
    links = review.get('source_link_defect_repairs', [])
    marks = review.get('choice_marker_repairs', [])
    assert isinstance(links, list) and isinstance(marks, list)
    if name == HOTEL['name']:
        assert len(links) == 1, (name, 'The proven hotel routing defect must be repaired explicitly')
    if name == ANGRY['name']:
        assert len(marks) == 1, (name, 'The proven missing-icon marker defect must be repaired explicitly')
    if not links and not marks:
        return
    assert review.get('structural_alignment'), (name, 'Source-defect repair needs an alignment record')
    for fix in links:
        assert name == HOTEL['name'] and english['kind'] == french['kind'] == 'compiled'
        assert fix.get('reason') and set(fix) >= {'passage', 'source_link', 'corrected_link', 'reason'}
        assert fix['passage'] == HOTEL['passage'] and fix['source_link'] == HOTEL['source_link'] and fix['corrected_link'] == HOTEL['corrected_link']
        for src in (english, french):
            assert _passage(src, fix['passage'])['_link'] == fix['source_link'], (name, 'Source routing changed')
            perfect = _passage(src, 'ca-goto-hotel-perfect')
            average = _passage(src, 'ca-goto-hotel-average')
            assert perfect['_variable_modifiers'] == ['hotel=5'] and average['_variable_modifiers'] == ['hotel=1']
            assert _passage(src, 'ca-hotel-perfect')['_link'].startswith('trust=1?ca-end-great')
    for fix in marks:
        assert name == ANGRY['name'] and fix.get('reason')
        assert (fix['passage'], fix['choice_index'], fix['source_marker'], fix['corrected_marker']) == (ANGRY['passage'], ANGRY['choice_index'], ANGRY['source_marker'], ANGRY['corrected_marker'])
        for src in (english, french):
            c = _passage(src, fix['passage'])['_choices'][fix['choice_index']]
            assert emote(c['_text']) == fix['source_marker']
        config = read_json(ROOT / 'review/original_choice_emoticon_config.json')
        triggers = {x['trigger_name'] for x in config['configuration']}
        assert fix['corrected_marker'].strip(':') in triggers and fix['source_marker'].strip(':') not in triggers


def corrected_link(reviewed, title, link):
    for fix in reviewed.get('french_review', {}).get('source_link_defect_repairs', []):
        if fix['passage'] == title:
            assert fix['source_link'] == link
            return fix['corrected_link']
    return link


def expected_marker(review, title, index, source_marker):
    for fix in (review or {}).get('choice_marker_repairs', []):
        if fix['passage'] == title and fix['choice_index'] == index:
            assert fix['source_marker'] == source_marker
            return fix['corrected_marker']
    from french_choice_markers import expected_marker as french_expected_marker
    marker = french_expected_marker(review, title, index, source_marker)
    from tps_mathieu_french_technical import expected_marker as tps_mathieu_marker
    marker = tps_mathieu_marker(review, title, index, marker)
    from mathieu_followup_markers import expected_marker as mathieu_followup_marker
    marker = mathieu_followup_marker(review, title, index, marker)
    from french_script_choice_markers import expected_marker as script_marker
    return script_marker(review, title, index, marker)
