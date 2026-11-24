"""Exercise saved rules with deliberate errors, without altering translations."""
import re
import hashlib
from datetime import datetime
from pathlib import Path
from check_consistency import RULES, check_scene
from corpus import ROOT, read_json, write_json


def probe(rules, scene, original, target):
    source = dict(name=scene, passages=[dict(_title='probe', _lines=[original])])
    review = dict(passages=[dict(passage='probe', texts=[target], choices=[])])
    return check_scene(source, review, rules)


def main():
    rules = read_json(RULES)
    results = []
    pairs = {'сказала': 'сказал', 'сказал': 'сказала'}
    for character in rules['characters']:
        probe_scene=(character.get('agreement_scenes') or ['probe'])[0]
        for pattern in character.get('forbidden_direct_agreement', []):
            head = re.match(r'^([А-Яа-яЁё-]+)', pattern).group(1)
            bad = re.search(r'сказала|сказал', pattern).group()
            # Test the first name and, where the rule declares it, the surname.
            surname = re.search(r'\(\?:((?:\\s\+[А-Яа-яЁё-]+)+)\)\?', pattern)
            suffix = ' '.join(re.findall(r'\\s\+([А-Яа-яЁё-]+)', surname.group(1))) if surname else ''
            names = [head] + ([head + ' ' + suffix] if suffix else [])
            # A required surname distinguishes passengers sharing a first name.
            required = re.match(r'^([А-Яа-яЁё-]+)\\s\+([А-Яа-яЁё-]+)\\s\+', pattern)
            if required:
                names = [required.group(1) + ' ' + required.group(2)]
                # Check surname exclusion for this pattern alone. Another pattern
                # of the same character may legitimately guard the first name.
                isolated = dict(rules)
                isolated['characters'] = [dict(c, forbidden_direct_agreement=[pattern])
                    if c['key'] == character['key'] else c for c in rules['characters']]
                for text in [head + ' ' + bad + '.', head + ' Другой ' + bad + '.']:
                    findings = probe(isolated, probe_scene, 'A passenger speaks.', text)
                    results.append(dict(rule=character['key'], text=text,
                                        expected_conflict=False,
                                        passed=not any(f['rule'] == character['key'] for f in findings)))
            for name in names:
                for text, expected in [(name + ' ' + bad + '.', True),
                                       (name + ' ' + pairs[bad] + '.', False)]:
                    findings = probe(rules, probe_scene, 'A passenger speaks.', text)
                    actual = any(f['rule'] == character['key'] for f in findings)
                    results.append(dict(rule=character['key'], text=text,
                                        expected_conflict=expected, passed=actual == expected))
        for sample in character.get('extra_agreement_probes',[]):
            findings=probe(rules,sample.get('scene',probe_scene),'A passenger speaks.',sample['text'])
            actual=any(f['rule']==character['key'] for f in findings)
            passed=actual==sample['expected_conflict']
            if sample.get('expected_other_rule'):
                passed &= any(f['rule']==sample['expected_other_rule'] for f in findings)
            results.append(dict(rule=character['key'],text=sample['text'],
                expected_conflict=sample['expected_conflict'],passed=passed,
                scope='Exact other-person name span; mixed blocks still guard this character.'))
    for obj in rules['objects']:
        object_probes={
            'notebook':('He takes out his notebook.', [('Он достаёт блокнот.',False),('Он достаёт тетрадь.',True),('Он достаёт ежедневник.',True),('Он достаёт ноутбук.',True)]),
            'paperback':('He takes out a paperback.', [('Он достаёт книгу в мягкой обложке.',False),('Он достаёт брошюру.',True),('Он достаёт блокнот.',True),('Он достаёт тетрадь.',True)]),
            'suitcase':('He retrieves her suitcase.', [('Он достаёт её дорожную сумку.',False),('Он достаёт её чемодан.',True),('Он достаёт её пакет.',True),('Он достаёт её багаж.',True)]),
            'file folder':('He finds a file folder.', [('Он находит папку.',False),('Он находит файловую папку.',True),('Он находит блокнот.',True),('Он находит досье.',True)]),
            'dagger':('He raises his dagger.', [('Он поднимает кинжал.',False),('Он поднимает нож.',True),('Он поднимает меч.',True),('Он поднимает кольцо.',True)]),
            'carrier':('He carries a baby carrier.', [('Он несёт детскую люльку.',False),('Он несёт перевозчика.',True),('Он несёт носителя.',True),('Он несёт детское кресло.',True),('Он несёт переноску.',True),('Он несёт гроб.',True)]),
            'infant seat':('He secures the infant seat.', [('Он закрепляет люльку.',False),('Он закрепляет кресло.',True),('Он закрепляет переноску.',True),('Он закрепляет гроб.',True)]),
            'fur':('She runs a hand through her fur.', [('Она проводит рукой по меху.',False),('Она поправляет меховой воротник.',False),('Она проводит рукой по шерсти.',True)]),
            'sleigh':('He searches for his sleigh.', [('САНТА: «Мои сани».',False),('Он говорит о санях.',False),('Он подходит к саням.',False),('Он ищет следы саней.',False),('Он приехал с санями.',False),('САНТА: «Мой транспорт».',True),('САНТА: «Повозка».',True),('Он ищет санки.',True),('САНТА: «Моя телега».',True)]),
            'bag':('She drags her shopping bag.', [('Она тащит хозяйственную сумку.',False),('Она роется в сумке.',False),('Она тащит дорожную сумку.',True),('Она тащит чемодан.',True),('Она несёт пакет.',True),('Она открывает кошелёк.',True),('Она тащит рюкзак.',True),('Она тащит саквояж.',True)]),
            'gun':('He compares this gun with the rifle he used in his youth.', [('Он держит пистолет.',False),('Он смотрит на оружие.',False),('Он сравнивает пистолет с винтовкой, которую ему выдали в молодости.',False),('Он держит ружьё.',True),('Он держит винтовку.',True),('Он достаёт нож.',True)]),
            'pump':('You pay directly at the pump.', [('Вы платите прямо у колонки.',False),('Вы платите у бензоколонки.',False),('Вы платите у насоса.',True),('Вы платите на помпе.',True),('На заправке «Ла Бель Помп» вы платите у колонки.',False)]),
            'pumps':('There is a smell between the pumps.', [('Между колонками чем-то пахнет.',False),('Между заправочными колонками пахнет мочой.',False),('Между насосами чем-то пахнет.',True)]),
            'boiler':('You return to the boiler.', [('Вы поворачиваетесь к водонагревателю.',False),('Вы подходите к котлу.',True),('Вы проверяете бойлер.',True),('Вы включаете обогреватель.',True)]),
            'water heater':('Boxes under the water heater.', [('Коробки под водонагревателем.',False),('Коробки под котлом.',True),('Коробки под бойлером.',True),('Коробки под обогревателем.',True)]),
            'receipt':('You take the receipt.', [('Вы берёте квитанцию.',False),('Вы прячете расписку.',True),('Вы берёте чек.',True),('Вы берёте список.',True)]),
            'list':('You take the list of customers.', [('Вы берёте список клиентов.',False),('Вы прячете список за спиной.',False),('Перед вами списки клиентов.',False),('Вы берёте квитанцию.',True),('Вы прячете расписку.',True),('Вы берёте чек.',True)])}
        object_probes['phil_fictional_half_sister_fr']=('He mentions his half-sister.', [('Она мне сестра только наполовину, но люблю я её всем сердцем.',False),('У нас один общий родитель.',False),('Сестра по одному родителю.',False),('Это моя сводная сестра.',True),('Это единокровная сестра.',True),('Это единоутробная сестра.',True),('У нас общая мать.',True),('Она сестра наполовину, у нас общий отец.',True),('Это родная сестра.',True)])
        object_probes['fragonard04_ethan_alias']=('His name was Ethan.', [('Его звали Этан.',False),('Они ушли с Этаном.',False),('Его звали Эрван.',True),('Этан, которого раньше звали Эрван.',True)])
        object_probes['kader_cousins_in_group_fr']=('Even the cousins in uniform.', [('Даже свои в форме.',False),('Даже своих не узнать.',False),('Даже кузены в форме.',True),('Даже свои двоюродные братья.',True),('Даже свои арабы.',True),('Даже своя братва в форме.',True)])
        object_probes['judge_shell_case']=('She examines the photo of the shell.', [('Она смотрит на фотографию гильзы.',False),('Гильза целая, пули тоже в порядке.',False),('Она смотрит на фото раковины.',True),('Она осматривает снаряд.',True),('Она смотрит на фотографию.',True)])
        for key,source_term in [('ade01_justice_fr','Judgment'),('ade02_justice_fr','Judgment'),('ade_justice_fr','Justice')]:
            object_probes[key]=('ADE: '+source_term+'.', [('АДЕ: «Справедливость».',False),('АДЕ: «Суд».',True),('АДЕ: «Правосудие».',True),('АДЕ: «Фортуна, Сила, Справедливость, Император».',False)])
        object_probes['camille_psychanalyse_fr']=('CAMILLE: Basics of psychotherapy?', [('КАМИЙ: «Основы психоанализа-а-а?»',False),('КАМИЙ: «Основы психоанализа?»',False),('КАМИЙ: «Основы психотерапии?»',True),('КАМИЙ: «Основы психиатрии?»',True),('КАМИЙ: «Психоанализ и психотерапия?»',True)])
        object_probes['chiara_takoyaki_stall_fr']=('CHIARA: I will open a takoyaki food truck.', [('Кьяра откроет ларёк с такояки.',False),('Кьяра откроет ларек с такояки.',False),('Кьяра мечтает о ларьке с такояки.',False),('Кьяра откроет фургон с такояки.',True),('Закусочная на колёсах с такояки.',True),('Кьяра откроет ларёк на колёсах.',True),('Кьяра откроет лавочку с такояки.',True)])
        object_probes['driver_notebook_mireille']=('You nod, put your notebook away, then turn the key.', [('Вы убираете документы.',False),('Вы убрали документы.',False),('Вы занимаетесь документом.',False),('Вы убираете блокнот.',True),('Вы убираете тетрадь.',True),('Вы убираете ежедневник.',True),('Вы убираете ноутбук.',True)])
        object_probes['carlo_beekeeper_fr']=('CARLO: My old friend is an entomologist.', [('Он стал пчеловодом.',False),('Он ПЧЕЛОВОД.',False),('Он стал энтомологом.',True),('Он дентомолог.',True),('Он пчеловодитель.',True),('Он стоматолог.',True)])
        object_probes['carlo_beekeeper_wrong_fr']=('CARLO: My friend became a dentomologist.', [('Он пчеловодитель.',False),('Он стал пчеловодителем.',False),('Пчело… пчеловодитель?',False),('Он пчеловод.',True),('Он дентомолог.',True),('Он энтомолог.',True)])
        object_probes['tps_magic_dagger']=('You draw the dagger from its sheath.', [('Вы достаёте кинжал.',False),('Вы вынимаете кинжал из ножен.',False),('Кинжал лежит в ножнах.',False),('Вы держите кинжал вместе с ножнами.',False),('Вы достаёте нож.',True),('Вы достаёте кинжал и нож.',True),('Вы касаетесь ножа и кинжала.',True),('Вы сражаетесь ножом, а не кинжалом.',True),('Кинжал похож на ножи.',True),('Вы достаёте меч.',True)])
        object_probes['tps_body_coils_fr']=('You feel the ring of his hands tighten.', [('Кольца его тела сжимаются.',False),('Он сдавил вас кольцами своего тела.',False),('Его хватка держит вас кольцами змеиного тела.',False),('Вы в плену колец его тела.',False),('Кольцо его рук сжимается.',True),('Кольцо ДЭМ сжимается.',True),('Перстень сияет.',True)])
        object_probes['tps_prepa_fr']=('We have college applications to fill out.', [('Нас ждут подготовительные классы.',False),('Надо учиться в подготовительных классах.',False),('Потом будут подготовительные классы.',False),('Надо подать заявления в вуз.',True),('Нас ждёт поступление в университет.',True)])
        object_probes['sylvie_cup_of_tea_idiom']=('Opera is not his cup of tea.', [('Опера не по его вкусу.',False),('Он не увлекается оперой.',False),('Он не любитель оперы.',False),('Это не его чашка чая.',True),('Опера не по его вкусу, он предпочитает чашку чая.',True),('Он не любит чай.',True)])
        object_probes['sean_bank_card_fr']=('Luckily I still have my credit card and papers.', [('Банковская карта и документы остались.',False),('Он сохранил банковскую карту.',False),('Кредитка и документы остались.',True),('Банковская кредитная карта осталась.',True),('Дебетовая банковская карта осталась.',True),('Карточка школы осталась.',True)])
        object_probes['francine_suffocating_care_fr']=('I might have been a little suffocating.', [('Я, может, душила его своей заботой.',False),('Я душила его своей любовью.',False),('Моя забота не давала ему свободы.',False),('Я его задушила.',True),('Я его душила.',True),('Ему нечем было дышать.',True)])
        object_probes['djena_priority_not_emergency_fr']=('But her case wasn’t an emergency.', [('Но она не была в приоритете.',False),('В первую очередь пришлось заниматься другими.',False),('Другие срочные случаи были в приоритете.',False),('Её случай не был экстренным.',True),('Помощь ей не требовалась.',True),('Её случай не был экстренным, она не была в приоритете.',True)])
        object_probes['jonas_creator_informaticien_fr']=('Our God is just a computer program.', [('Наш Бог — компьютерщик.',False),('Это специалист по информатике.',False),('Бог — не компьютерная программа, а компьютерщик.',False),('Наш Бог — компьютерная программа.',True),('Бог — программа.',True),('Создатель — код.',True)])
        object_probes['vero_gros_yeux_fr']=('Her eyes widen in dismay.', [('Она смотрит с неодобрением.',False),('Она смотрит недовольно.',False),('В её взгляде укор.',False),('Она в растерянности распахивает глаза.',True),('Её глаза расширились от удивления.',True),('Она улыбается.',True)])
        object_probes['vero_farewell_emoji_fr']=('Woman Raising Hand emoji.', [('Эмодзи «женщина прощается».',False),('Эмодзи прощающейся женщины.',False),('Женщина прощается…',False),('Женщина с поднятой рукой.',True),('Подпись: эмодзи.',True),('Эмодзи «мужчина прощается».',True)])
        object_probes['amelie_career_duration_fr']=('Actresses could work for a dozen years or so, fifteen if they started young.', [('Добрых десять лет, пятнадцать — если начинала молодой.',False),('Десять лет, а при раннем старте — пятнадцать.',False),('10 лет, а если начинала молодой — 15.',False),('Двенадцать лет, пятнадцать при раннем старте.',True),('Дюжина лет, а при раннем старте — пятнадцать.',True),('12 лет, а при раннем старте — 15.',True),('Десять лет.',True),('10 лет, 15 при раннем старте;12 лет в другой версии.',True)])
        object_probes['agnes_surlesbords_fr']=('She is the reactionary Catholic kind.', [('У неё есть что-то от католических консерваторов старой школы.',False),('Она слегка правая католичка.',False),('Консервативная католичка по краям.',True),('Она старая католичка.',True),('Консерватор, по краям католичка.',True)])
        object_probes['agnes_no_lives_lost_idiom']=('No lives lost!', [('Ничего страшного!',False),('Не беда!',False),('Никто же не умер!',True),('Никто не погиб.',True),('Ничего страшного, никто не умер.',True)])
        object_probes['leia_usual_suspects_title']=('I just watched The Usual Suspects.', [('ЛЕЯ: «Фильм „Подозрительные лица“».',False),('Подозрительные лица',False),('ПОДОЗРИТЕЛЬНЫЕ ЛИЦА',False),('ЛЕЯ: «Обычные подозреваемые».',True),('Подозрительные лица, они же Обычные подозреваемые.',True),('Какой-то фильм про подозреваемых.',True)])
        object_probes['purva_fontainebleau_forest_fr']=('There’s a huge park, very wild. If he likes hiking…', [('Там огромный лес. Если он любит пешие прогулки, ему понравится…',False),('Огромный лес, ему понравится.',False),('Там парк, почти дикий.',True),('Там огромный лесопарк.',True),('Лес рядом с диким парком.',True)])
        object_probes['alaric_forged_door_fr']=('A frail figure comes out of the big, cast iron door.', [('Из большой кованой двери выходит хрупкая фигурка.',False),('За дверью из кованого железа появляется ребёнок.',False),('Из большой чугунной двери выходит мальчик.',True),('Ребёнок выходит через большую дверь.',True),('Кованая и чугунная дверь.',True)])
        probe_key=obj['key'] if obj['key'] in object_probes else obj['source_term']
        if probe_key not in object_probes:
            continue
        original,targets=object_probes[probe_key]
        for target, expected in targets:
            findings = probe(rules, obj['scene_prefix'] + 'probe',
                             original, target)
            actual = any(f['rule'] == obj['key'] for f in findings)
            results.append(dict(rule=obj['key'], text=target,
                                expected_conflict=expected, passed=actual == expected))
        # The owner's rule must not change another scene's unrelated object.
        findings = probe(rules, 'unrelated_probe', original,
                         'Он достаёт тетрадь.')
        results.append(dict(rule=obj['key'], text='unrelated owner',
                            expected_conflict=False,
                            passed=not any(f['rule'] == obj['key'] for f in findings)))
        if obj['key']=='leia_usual_suspects_title':
            for original,target in [('The ordinary suspects waited.','Обычные подозреваемые ждали.'),('The Unusual Suspects is a different title.','Другое название, не «Подозрительные лица».')]:
                findings=probe(rules,'013_leia_01',original,target)
                results.append(dict(rule=obj['key'],text=target,expected_conflict=False,passed=not any(f['rule']==obj['key'] for f in findings),scope='Exact original film title only; ordinary suspects/another title remain unrelated.'))
    # The same owner uses the different French Le Jugement card in07. A guard
    # for the EN/FR mismatch in01/02 must never replace that valid Суд.
    for scene,original,target in [
        ('025_ade_07','ADE: Judgment did not appear.','АДЕ: «Но Суд не выпал…»'),
        ('unrelated_probe','They are discussing Justice.','Они обсуждают правосудие.'),
    ]:
        findings=probe(rules,scene,original,target)
        results.append(dict(rule='ade_tarot_scope',text=target,expected_conflict=False,
            passed=not any(f['rule'] in ('ade01_justice_fr','ade02_justice_fr','ade_justice_fr') for f in findings),
            scope='Actual Le Jugement in Ade07 and another owner legal prose remain unaffected.'))
    for scene,original,target in [('unrelated_probe','Basics of psychotherapy.','Основы психотерапии.'),('053_camille_01','A psychotherapist replies.','Психотерапевт отвечает.')]:
        findings=probe(rules,scene,original,target)
        results.append(dict(rule='camille_psychanalyse_scope',text=target,expected_conflict=False,passed=not any(f['rule']=='camille_psychanalyse_fr' for f in findings)))
    for scene,original,target in [('unrelated_probe','A takoyaki food truck.','Закусочная на колёсах.'),('033_chiara_01','Her bicycle waits outside.','Велосипед с большими колёсами остался у ресторана.')]:
        findings=probe(rules,scene,original,target)
        passed=not any(f['rule']=='chiara_takoyaki_stall_fr' for f in findings)
        results.append(dict(rule='chiara_stall_scope',text=target,expected_conflict=False,passed=passed))
    failures = [r for r in results if not r['passed']]
    for scene,original,target in [('066_tps_01','The magic ring burns.','Кольцо ДЭМ обжигает палец.'),('021_carlo_01','He completes college applications.','Он подаёт заявления в вуз.'),('066_tps_01','The dagger lies in the sheath.','Кинжал лежит в ножнах.')]:
        findings=probe(rules,scene,original,target)
        results.append(dict(rule='tps_exact_owner_scope',text=target,expected_conflict=False,passed=not any(f['rule'] in ('tps_body_coils_fr','tps_prepa_fr','tps_magic_dagger') for f in findings)))
    for scene,original,target in [('004_jeannoel_01','You put your notebook away.','Вы убираете блокнот.'),('060_mireille_01','François opens his notebook.','Франсуа открывает блокнот.'),('060_mireille_01','Mireille retrieves her suitcase.','Мирей достаёт дорожную сумку.')]:
        findings=probe(rules,scene,original,target)
        results.append(dict(rule='mireille_documents_scope',text=target,expected_conflict=False,passed=not any(f['rule']=='driver_notebook_mireille' for f in findings),scope='Other scene notebook, different person notebook inside060, and Mireille luggage remain unaffected.'))
    for scene,original,target in [('027_lucieemilie_02','They mention a half-sister.','Они упомянули сводную сестру.'),('034_phil_01','His parents have a stepdaughter.','У родителей есть сводная дочь.')]:
        findings=probe(rules,scene,original,target)
        results.append(dict(rule='phil_half_sister_scope',text=target,expected_conflict=False,passed=not any(f['rule']=='phil_fictional_half_sister_fr' for f in findings)))
    failures = [r for r in results if not r['passed']]
    for scene,original,target in [('unrelated_probe','He drinks a cup of tea.','Он пьёт чашку чая.'),('035_sylvainsylvie_01','He drinks tea.','Он пьёт чай.')]:
        findings=probe(rules,scene,original,target)
        results.append(dict(rule='sylvie_tea_scope',text=target,expected_conflict=False,passed=not any(f['rule']=='sylvie_cup_of_tea_idiom' for f in findings)))
    failures = [r for r in results if not r['passed']]
    for scene,original,target in [('unrelated_probe','He keeps his credit card.','У него осталась кредитка.'),('036_sean_01','He keeps his student card.','У него остался студенческий билет.')]:
        findings=probe(rules,scene,original,target)
        results.append(dict(rule='sean_card_scope',text=target,expected_conflict=False,passed=not any(f['rule']=='sean_bank_card_fr' for f in findings)))
    failures = [r for r in results if not r['passed']]
    for scene,original,target in [('unrelated_probe','The smoke was suffocating.','Он задыхался в дыму.'),('041_francine_01','She hears someone suffocate.','Она слышит, как кто-то задыхается.')]:
        findings=probe(rules,scene,original,target)
        results.append(dict(rule='francine_care_scope',text=target,expected_conflict=False,passed=not any(f['rule']=='francine_suffocating_care_fr' for f in findings)))
    failures = [r for r in results if not r['passed']]
    for scene,original,target in [('unrelated_probe','But her case wasn’t an emergency.','Её случай не был экстренным.'),('042_djena_02','There were other emergencies.','Были другие срочные случаи.')]:
        findings=probe(rules,scene,original,target)
        results.append(dict(rule='djena_priority_scope',text=target,expected_conflict=False,passed=not any(f['rule']=='djena_priority_not_emergency_fr' for f in findings)))
    failures = [r for r in results if not r['passed']]
    for scene,original,target in [('unrelated_probe','He opens a computer program.','Он открывает компьютерную программу.'),('043_jonas_01','A program, if you like.','Программа, если хотите.')]:
        findings=probe(rules,scene,original,target)
        results.append(dict(rule='jonas_creator_scope',text=target,expected_conflict=False,passed=not any(f['rule']=='jonas_creator_informaticien_fr' for f in findings)))
    failures = [r for r in results if not r['passed']]
    for scene,original,target in [('045_vero_01','She raises her hand.','Она поднимает руку.'),('045_vero_01','Her eyes are smiling.','Её глаза улыбаются.')]:
        findings=probe(rules,scene,original,target)
        results.append(dict(rule='vero_exact_source_scope',text=target,expected_conflict=False,passed=not any(f['rule'] in ('vero_gros_yeux_fr','vero_farewell_emoji_fr') for f in findings)))
    failures = [r for r in results if not r['passed']]
    findings=probe(rules,'047_amelie_01','The actress is twelve years older.','Актриса старше на двенадцать лет.')
    results.append(dict(rule='amelie_career_scope',text='Different age statement in same scene',expected_conflict=False,passed=not any(f['rule']=='amelie_career_duration_fr' for f in findings)))
    failures = [r for r in results if not r['passed']]
    for scene,original,target in [('058_agnes_01','The painting is worn around the edges.','Краска по краям облупилась.'),('060_mireille_01','A little bohemian around the edges.','Слегка богемный.'),('058_agnes_01','Someone died yesterday.','Вчера кто-то умер.'),('unrelated_probe','No lives lost!','Никто не погиб!')]:
        findings=probe(rules,scene,original,target)
        results.append(dict(rule='agnes_idiom_source_scope',text=target,expected_conflict=False,passed=not any(f['rule'] in ('agnes_surlesbords_fr','agnes_no_lives_lost_idiom') for f in findings),scope='Actual spatial edges and actual death in another source slot; other-scene phrase does not trigger058rules.'))
    failures = [r for r in results if not r['passed']]
    findings=probe(rules,'014_fragonard_04','A different person named Erwan speaks.','Эрван говорит.')
    results.append(dict(rule='fragonard04_ethan_alias_scope',text='Different source person in same scene',expected_conflict=False,passed=not any(f['rule']=='fragonard04_ethan_alias' for f in findings)))
    for scene,original,target in [('061_denis_01','I called my cousin.','Я позвонил двоюродному брату.'),('056_kader_01','His girlfriend works in the police.','Его девушка работает в полиции.')]:
        findings=probe(rules,scene,original,target)
        results.append(dict(rule='kader_cousins_in_group_scope',text=target,expected_conflict=False,passed=not any(f['rule']=='kader_cousins_in_group_fr' for f in findings)))
    for scene,original,target,rule in [
        ('002_purva_01','A park beside the palace.','Парк возле дворца.','purva_fontainebleau_forest_fr'),
        ('003_alaric_01','The taxi door opens.','Дверь такси открывается.','alaric_forged_door_fr'),
        ('003_alaric_01','A cast iron statue stands outside.','Снаружи стоит чугунная статуя.','alaric_forged_door_fr')]:
        findings=probe(rules,scene,original,target)
        results.append(dict(rule=rule,text=target,expected_conflict=False,passed=not any(f['rule']==rule for f in findings),scope='Different actual source item in the same owner scene remains unaffected.'))
    from extra_localization import guard_probes
    results += guard_probes(rules)
    from passenger_call_defaults import guard_probes as call_default_probes
    results += call_default_probes()
    failures = [r for r in results if not r['passed']]
    write_json(ROOT / 'review/consistency_guard_validation.json',
               dict(checks=len(results), passed=not failures, results=results,
                    checked_at=datetime.now().astimezone().isoformat(),
                    rules_sha256=hashlib.sha256(RULES.read_bytes()).hexdigest(),
                    checker_sha256=hashlib.sha256((ROOT / 'scripts/check_consistency.py').read_bytes()).hexdigest(),
                    test_sha256=hashlib.sha256(Path(__file__).read_bytes()).hexdigest(),
                    scope='Synthetic direct-name agreement including hyphenated first/surname forms, exact homonym-span exclusions, and owner-scoped notebook/paperback/suitcase/file-folder/dagger/carrier/infant-seat/fur/sleigh/gun/bag rules including speaker-name/root collisions and a separate past rifle in the same valid line; pronouns and ownership reversals still require manual review.'))
    print('Consistency guard probes:', len(results), 'failures:', len(failures))
    for f in failures:
        print(f['rule'], f['text'])
    if failures:
        raise SystemExit(1)


if __name__ == '__main__':
    main()
