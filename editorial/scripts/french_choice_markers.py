"""Source-pinned French choice markers, including two EN-only silence markers.

Only existing game triggers are restored. No sprites, prose, menus or links are
created here. Further differences require their own explicit review.
"""
import hashlib
import json
from corpus import ROOT, read_json, command, emote

FIELD = 'french_choice_marker_restorations'
CASES = {
    '003_alaric_01': dict(
        english_sha256='c6a25626d47f1dfbedd1de079f7f72d16b3fb8d875021bb60316903031872e4f',
        french_sha256='1a129b1759df7f3ce49d60ff8d09b1a5fad9b344192f0c6cdeb95fd3c4088005',
        choices=[('alaric-mass02', 1, ':silence:')]),
    '004_jeannoel_02': dict(
        english_sha256='cc106ca950d1465f576db467c3e58480d648b78c6b32de08475d6171d2b2c9d9',
        french_sha256='aa091a92e0e0614052c4075626f29d89c906d438560ce7b512e785ca25152642',
        choices=[('jn-02-start', 0, ':positive:'), ('jn-02-start', 1, ':negative:')]),
    '006_esmeralda_02': dict(
        english_sha256='ecbd8fd6b5055b7c60e7e908870a6a8a251727a4a754e586e1db4e3c75841eea',
        french_sha256='8bf1a39445db4c5242e2a9486bfd1408074d3d30710a8b7289dd88efe4459671',
        source_marker=':silence:',
        choices=[('esmeralda-02-no01', 1, ''), ('esmeralda-02-no02', 2, '')]),
    '012_christophe_03': dict(
        english_sha256='18e7463a60212990b2c839c71b0105f22c5d16b2f5e39f444491a57f602a4c85',
        french_sha256='15fcb9ff5688ed4673b179d7c32bdc9b5b677035ee72d01f1b8e7fefcc040c3a',
        choices=[('chris-03-pregnant-baby', 1, ':silence:')]),
    '013_leia_05': dict(
        english_sha256='69f7ff9e14a617cb83903cc2c99dfa9ca03fb99a4f195a321158a69a7004b12b',
        french_sha256='1dad8ffb6faa10a3d3c296eb89c1452492b771ba0d16d6936a2bf813667b7fd0',
        source_marker=':silence:',
        choices=[('leia-05-start-answer', 2, ''), ('leia-05-moved', 2, '')]),
    '015_alicia_03': dict(
        english_sha256='dd256931e4d88133522df5da251147a67e27d9ddc8ed3a7e8a7e3ceed4f7f9be',
        french_sha256='db8b4b1c3b012450c18cc952465f56a00ad185876d346c7c4e76a2f9f6e0f4aa',
        source_marker=':silence:',
        choices=[('alicia-03-elevenyears', 1, '')]),
    '013_leia_01': dict(
        english_sha256='1f0c73ddef871193dcbc28afe5330756ac7bed45e8be594d12cd80199d953b5c',
        french_sha256='857fe9da423012307abaac64979be1c2461fcfb86c69376f9f5ec0dcdc595e24',
        choices=[('leia-01-start', 1, ':silence:'), ('leia-01-shock', 1, ':silence:')]),
    '013_leia_08': dict(
        english_sha256='21028bfccdb8466dec634c9c3cc5d5400ca84074f3936de2448edd8571468cef',
        french_sha256='b5a437e0ac929ed955da01ebee2859d2c2ddd44a30c032668dbefb02fbb9f49f',
        source_marker=':silence:',
        choices=[('leia-08-mind', 3, ''), ('leia-08-meaning', 2, ''), ('leia-08-festival', 3, '')]),
    '014_fragonard_08': dict(
        english_sha256='cd2a73f47c2b12ce2360f5c7efc272d9cf808b4d7c05f9c32e1c6d2acce91315',
        french_sha256='08ce03aa001ee8a87ec7dc076c1cdd85795f8fc5b5aa986e8a45048a7db2c264',
        source_marker=':positive:',
        choices=[('frag-08-story-start', 2, '')]),
    '019_herve_04': dict(
        english_sha256='0ba1e7c8c7df43632bd151b3637ec572cd8649d38e4d008af6b3718ff7d15c53',
        french_sha256='df97811556826601191d03a061f77e97b0bfec938aebf708002344d4b874ff92',
        source_marker=':silence:',
        choices=[('herve-04-sleep-start', 1, '')]),
    '019_herve_05': dict(
        english_sha256='49b1c30088f93490fa73e6e39fd3e948d43ac59892ba1c45eea213cb156d4921',
        french_sha256='411ab075a2995d40615a8811130e29f41ef162a60acffc3d5099201cf9fe856c',
        choices=[('herve-05-intro', 2, ':silence:')]),
    '023_apollonie_01': dict(
        english_sha256='2399f9f29bb1a3e56f5eba8467a5395d8a0c6f91f152686a2def3e9a908d7606',
        french_sha256='584376149b016ef8bb137b5b39270dcff3c33191e60d3166d91e666a74077a4a',
        source_marker=':silence:',
        choices=[('apollonie-drive', 2, '')]),
    '025_ade_07': dict(
        english_sha256='c42b347955a4421d38c03d58b2207f3ebd9987095083604e12b0803cbdc96b88',
        french_sha256='7fbd63af8c3e628a1d8f4c2414824069b2790ecb3f57a864dd1bd18e496f8ca5',
        choices=[('ade-07-no', 2, ':silence:')]),
    '029_jerome_02': dict(
        english_sha256='6820ca7d2d94953f0c6a3d30565f24c7a37e847ecfc7fa8af46ef1c58b28c88d',
        french_sha256='a5a2802a9b7214190debbaa6ca71d84b05b690ab3da06a4de1a6101f8adc2cbf',
        source_marker=':silence:',
        choices=[('jerome-02-start2', 1, ''), ('jerome-02-sickness3', 1, ''), ('jerome-02-chase3-hub', 3, '')]),
    '037_shinji_01': dict(
        english_sha256='199bbdfd623600ddcbd0f8e5f48449d1a06356a5642489d5a021b28067706a88',
        french_sha256='5ea8a0834e7fc64c4e6a2021665bcee97c50241777451cf894793063c83a860c',
        choices=[('shinji-01-silence1', 2, ':silence:'), ('shinji-01-outside', 3, ':silence:')]),
    '040_crouky_01': dict(
        english_sha256='44a46f227f8d072d632d7734d1f801af2cddcce22739ebaed4b1de2240fc580c',
        french_sha256='eec05510c116789eac3d9ae1070013848526d99e29921b267f1186770807f400',
        choices=[('crouky-owner-stick', 1, ':silence:')]),
    '047_amelie_01': {'english_sha256': 'e0718e9cfac6e1e98f8ff2b97c78bf96be4e0a89c9581c2b160622e28c0e26db', 'french_sha256': 'd67008296818f5793eb7586f41b07bd93ca0ba4a03b8e7df2ec4fd34513a6dfa', 'source_marker': ':silence:', 'choices': [('amelie-01-inquiry-truth', 2, ''), ('amelie-01-inquiry-men', 1, ''), ('amelie-01-inquiry-bi', 1, ''), ('amelie-01-inquiry-format', 1, '')]},
    '053_camille_01': {'english_sha256': '308b2cea08c11607623798589fb8105b95b58bd559e1138f36d01444db164b44', 'french_sha256': 'bf661e77c180036b87a17707501bcc5d142eb836fa69d28bcd5034705d25b32e', 'source_markers': {('camille-01-start-shout', 3): ':silence:'}, 'choices': [('camille-01-start-shout', 3, ''), ('camille-01-question1-stop', 3, ':taxi:'), ('camille-01-question1b', 2, ':taxi:'), ('camille-01-question2', 2, ':taxi:'), ('camille-01-question4', 1, ':taxi:'), ('camille-01-question5', 0, ':taxi:'), ('camille-01-idea', 3, ':taxi:')]},
    '054_ludivine_01': {'english_sha256': 'd9ef6359afe9d2f3c694c58b6d1d781ad19ae84dd9123a3fc9322e88a0a1f9eb', 'french_sha256': '401f74072e6aa925e9de4b28c024c0a42c6713296d8f289cc06e7a78e50d6f6d', 'choices': [('ludivine-01-getout', 3, ':silence:')]},
    '056_kader_01': {'english_sha256': '7568e39cba1e3b7bfdad4d66bb1e68a64c29702fd0603431c94860ac268792ad', 'french_sha256': 'c6faa9ced0c4b0b17f9d75148a829b6cfb2df69e7c03e60008a721865658b075', 'source_marker': ':silence:', 'choices': [('kader-01-woman-others', 2, '')]},
    '063_pauline_01': {'english_sha256': 'ee458c5d108fa6015c2f00c08b0922a34a74b07f5ac9be7bb24e7f65bbb5d64a', 'french_sha256': '54ba1129c7a864256b67bb923cad8ab3cf7773ca5ddd642e9fec12d931e78747', 'source_marker': ':silence:', 'choices': [('pauline-01-love8', 2, '')]},
    '075_henri_01': {'english_sha256': '7fc72f9d366ba0136b3506530307c842a9ebd05769b745a0421ba80c8f16c7d3', 'french_sha256': '56916c4e8b49d75dc599a17a96fcea0a42755d7e1327d14a1f122cee922bad9b', 'source_marker': ':silence:', 'choices': [('henri-01-silence', 3, '')]},
    '076_angelique_02': {'english_sha256': '3446a2f07db2124f645bf9c8cde86629021d73d4f754733502bd6f19cee9678f', 'french_sha256': '857753b7654817444d49f46878ed333b04c88b7e49217258bdb1696925461cc4', 'source_marker': ':silence:', 'choices': [('angel-02-arrival', 2, '')], 'command_slots': [{'passage': 'angel-02-son', 'index': 7, 'before': ['ANGÉLIQUE : « And old lady who swears and calls everyone… »', '$$ anim-link: TIC01 $$'], 'after': ['$$ anim-link: TIC01 $$', 'ANGÉLIQUE : « And old lady who swears and calls everyone… »']}]},
    '076_angelique_03': {'english_sha256': '77285a204c05780a7dfd0b55e5e638a3db757757e6318b85019a21b1b117b69e', 'french_sha256': '1a1dff483798bbfb4f57dcc16e3e732f7cab9e596601b8615b69998a52aa3d37', 'source_marker': ':silence:', 'choices': [('angel-03-laughing', 3, '')], 'command_slots': [{'passage': 'angel-03-laughing', 'index': 6, 'before': ['Silence. You scan her face to try to determine how serious she is…', '$$ anim-link: TIC03 $$'], 'after': ['$$ anim-link: TIC03 $$', 'Silence. You scan her face to try to determine how serious she is…']}]},
    '077_santa_01': {'english_sha256': '0c836a5bb57df90683506bbfc88e09e26b73b2820637923b54290d2ac4ae22a6', 'french_sha256': 'e2432eafc29d5d8269dd76f04912c0068f02e4125de139608fb6c2c0d3df6a5f', 'choices': [], 'command_replacements': [{'passage': 'santa-01-driver1', 'index': 3, 'before': '$$ movie $$', 'after': '$$ movie: black $$'}]},
    '090_houssine_06': {'english_sha256': '9f28852c761d939104183635fc3b799c5f71442a96ad7aa1854e14571764c1a3', 'french_sha256': '6b2fbf5bbce303e03d48e4c6710cef431de61d94bba28e04a5ae5043c98ec531', 'source_marker': ':silence:', 'choices': [('houssine-06-dontknow', 2, '')]},
    '097_alexandre_02': {'english_sha256': '897dbbc61421f932ec68af0b7dfd2b403de6787d81b90b525ab4000fbdc884f3', 'french_sha256': '424eef233d5438a2fc4c934ac860b3a3313517f6f6ff695b9ce6a276ed27a5c5', 'source_marker': ':violence:', 'choices': [('alexandre-02-boetie-story01-cop-confess-hubB', 1, '')], 'command_replacements': [{'passage': 'alexandre-02-arrest-10', 'index': 4, 'before': '$$ sfx: sfx_gameplay_car_open_from_int $$', 'after': '$$ sfx: sfx_gameplay_car_open_from_ext $$'}]},
    '099_boss_01': {'english_sha256': 'd6349fcce1484fde15dd05eac4bd1e976918962b5f6a1a67a75c64802ea87b52', 'french_sha256': '633a44f1c9ee66037a274c3d90a8c17864f4e935fe9aa35efdf7632cac944f82', 'choices': [], 'command_slots': [{'passage': 'boss-01-worried-laius', 'index': 2, 'before': ['Your boss smiles. He knows he’s told you the story a million times.'], 'after': ['$$ music-control: 40 $$', 'Your boss smiles. He knows he’s told you the story a million times.']}, {'passage': 'boss-01-worried3', 'index': 4, 'before': ['$$ music-control: 40 $$'], 'after': []}]},
    '100_cop_04': {'english_sha256': '5de34892a4f71d457d98843378dd00c979b4c437309b5801affab385e7379b78', 'french_sha256': '9f74038d921c11139ad4c30253d59edf3e9787744a158276042c4749d9615101', 'choices': [('cop-04-criminal', 2, ':silence:')]},
    '203_milo_02': {'english_sha256': '0051e444f7b2c3aed111a1f2493d1662d92c79e3be320eb6bfd621e4ad8e4a74', 'french_sha256': 'b8c1d77cc71b72b4527f73f4012787609abd8e8a7219c95f6b5fefb1f5c12af4', 'source_marker': ':silence:', 'choices': [('milo-02-africa02', 2, '')]},
    '208_myrtille_02': {'english_sha256': 'a1b3b48018d162b016f3a0843d0a688bec29358a8114562b8d386b9d71b76ed0', 'french_sha256': 'ea2492d56a3120f3100d71c2377f58a86dd76449bdacad6ca13554f607a5dbbf', 'source_marker': ':silence:', 'choices': [('myrtille-02-answer-outro', 2, '')]},
    '214_roksana_01': {'english_sha256': 'edf47aed164b70c8d7a494e913b819bbfb607e1311d554c264896964e9b521ec', 'french_sha256': '8444570d12239194d7d89fe15b436073014e0624b012e1124bc78b84143a0bb1', 'source_marker': ':silence:', 'choices': [('ludwig-chat-known', 2, ''), ('ludwig-chat-unknown', 3, ''), ('roksana-intro-ludwigunknown', 2, '')]},
    '222_melchior_02': {'english_sha256': '417162802f75b6ef46fbc75a8a933e740e4091baafe772bb85e41be11965f49d', 'french_sha256': '4a30de89464f639f863c6ef49c15a03f4ba0f315b428f4871bd1546c9badb94a', 'source_marker': ':silence:', 'choices': [('melchior-02-outro-destination', 2, '')]},
}


def expected_marker(review, title, index, source_marker):
    for fix in (review or {}).get(FIELD, []):
        if (fix['passage'], fix['choice_index']) == (title, index):
            assert source_marker == fix['source_marker']
            return fix['french_marker']
    return source_marker


def validate(name, review, english, french):
    fixes = review.get(FIELD, [])
    assert isinstance(fixes, list)
    if name not in CASES:
        assert not fixes, (name, 'No reviewed French marker restoration for this scene')
        return
    case = CASES[name]
    assert len(fixes) == len(case['choices']), (name, 'All accepted source marker decisions must remain explicit')
    assert review.get('structural_alignment')
    assert english['kind'] == french['kind'] == 'compiled'
    assert english['original_file'] == french['original_file']
    raw = read_json(ROOT / english['original_file'])
    assert raw['m_Name'] == name
    originals = {r['lang']: r['dialog'] for r in raw['dialogs']}
    for lang, src, key in [(1, english, 'english_sha256'), (0, french, 'french_sha256')]:
        digest = hashlib.sha256(json.dumps(originals[lang], ensure_ascii=False, sort_keys=True).encode('utf8')).hexdigest()
        assert digest == src['source_sha256'] == case[key]
        assert src['passages'] == originals[lang]['_passages'], 'Source object must equal immutable original'
    assert originals[0]['_variables'] == originals[1]['_variables']
    ep = {p['_title']: p for p in english['passages']}
    fp = {p['_title']: p for p in french['passages']}
    assert ep.keys() == fp.keys()
    triggers = {r['trigger_name'] for r in read_json(ROOT / 'review/original_choice_emoticon_config.json')['configuration']}
    for fix, (title, index, marker) in zip(fixes, case['choices']):
        assert set(fix) == {'passage', 'choice_index', 'source_marker', 'french_marker', 'reason'}
        assert type(fix['choice_index']) is int
        source_marker = case.get('source_markers', {}).get((title, index), case.get('source_marker', ''))
        assert (fix['passage'], fix['choice_index'], fix['source_marker'], fix['french_marker']) == (title, index, source_marker, marker)
        assert isinstance(fix['reason'], str) and fix['reason'].strip()
        assert not marker or marker.strip(':') in triggers
        assert emote(ep[title]['_choices'][index]['_text']) == source_marker
        assert emote(fp[title]['_choices'][index]['_text']) == marker
    for title, p in ep.items():
        q = fp[title]
        assert {k: v for k, v in p.items() if k not in ['_lines', '_choices']} == {k: v for k, v in q.items() if k not in ['_lines', '_choices']}
        # Commands retain their exact positions relative to every narrative slot.
        skeleton = lambda lines: [s if command(s) else '<TEXT>' for s in lines]
        from french_command_alignment import source_lines as aligned_command_lines
        from ludivine_french_gratitude import technical_lines as gratitude_slots
        assert skeleton(aligned_command_lines(name,review,p)) == skeleton(gratitude_slots(name,review,p,q)), (name, title, 'Other command/line differences need separate review')
        assert len(p['_choices']) == len(q['_choices'])
        for i, (a, b) in enumerate(zip(p['_choices'], q['_choices'])):
            assert {k: v for k, v in a.items() if k != '_text'} == {k: v for k, v in b.items() if k != '_text'}
            assert expected_marker(review, title, i, emote(a['_text'])) == emote(b['_text'])



def source_lines(name, review, passage, lines):
    """Restore only reviewed FR marker-associated animation/text boundaries."""
    slots = (review or {}).get('french_marker_command_slots', [])
    expected = CASES.get(name, {}).get('command_slots', [])
    assert isinstance(slots, list) and slots == expected, (name, 'Exact marker-associated command slots required')
    result = list(lines)
    for slot in slots:
        assert type(slot['index']) is int
        if passage['_title'] == slot['passage']:
            i = slot['index']
            width = len(slot['before'])
            assert width > 0 and result[i:i+width] == slot['before'], (name, 'Original slot anchor changed')
            result[i:i+width] = slot['after']
    replacements = (review or {}).get('french_marker_command_replacements', [])
    assert isinstance(replacements, list) and replacements == CASES.get(name, {}).get('command_replacements', []), (name, 'Exact reviewed command parameters required')
    for fix in replacements:
        assert type(fix['index']) is int
        if passage['_title'] == fix['passage']:
            i = fix['index']
            assert result[i] == fix['before'], (name, 'Original command anchor changed')
            result[i] = fix['after']
    return result
