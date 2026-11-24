from corpus import *
from datetime import date

def main():
    mapping = read_json(ROOT/'revision/data/Russian_UI/full_translation_mapping.json')
    rows = []
    rules=read_json(ROOT/'localization/consistency.json')
    facts={c['key']:c for c in rules['characters']}
    overrides = {'Peter "DJ Watson" Covelo':'Питер «Диджей Уотсон» Ковело',
        'Claudia Campos':'Клаудия Кампос', 'Julian Paz':'Жюльян Паз',
        'Patricia Hossein':'Патрисия Хоссейн', 'Francine Gayard':'Франсин Гайар',
        'Lieutenant Ninon Busset':'Лейтенант Нинон Бюссе'}
    context_keys={'001_patricia','002_purva','003_alaric','040_crouky','059_pierrot'}
    for x in read_json(ROOT/'original/inventory.json'):
        if x['cls']!='PassengerObjectScript':
            continue
        d = read_json(ROOT/x['file'])
        eng = d.get('fullname','')
        if not eng or eng=='???':
            continue
        ru = overrides.get(eng, mapping.get(eng,''))
        assert ru, eng
        ru = ru.replace(' & ',' и ')
        fact=facts.get(d['m_Name'])
        if fact:ru=fact['russian']
        if any(r['english']==eng for r in rows):
            continue
        rows.append(dict(category='character',key=d['m_Name'],english=eng,russian=ru,
            status='reviewed' if fact or eng in overrides or d['m_Name'] in context_keys else 'inherited_needs_context_review',
            gender=fact.get('gender','not_confirmed') if fact else 'not_confirmed',
            gender_label=fact.get('gender_label') if fact else None,
            note='Не подменять ключ пассажира его именем. Склонять имя по контексту.'+
                (' '+fact['pronouns']+'. '+fact['notes'] if fact else '')))
    terms = [
        ('Crouky','Круки','Кот; мужской род. Имя не склоняется.'),
        ('Pierrot Bataz','Пьеро Батаз','Полицейский. Пьеро — имя оригинала, не Пьер.'),
        ('Saint-Lazare / Gare Saint-Lazare','вокзал Сен-Лазар','На вокзал Сен-Лазар; у вокзала Сен-Лазар.'),
        ('Groupe Diamant','группа «Диамант»','Название компании; Groupe — французское слово «группа».'),
        ('Albatros','«Альбатрос»','Название здания, в котором произошёл пожар.'),
        ('Rictodin','«Риктодин»','Название препарата. Не терять связь с медицинским скандалом.'),
        ('medical examiner','судебно-медицинский эксперт','В досье Клаудии; не «медицинский эксперт».'),
        ('retired mailman','почтальон на пенсии','Александр Леклер. Не относится к Пьеро.'),
        ('the Moon Flesh cult','культ «Лунная плоть»','В досье Пьеретт.'),
        ('fare / tip','плата за проезд / чаевые','Не смешивать выручку за поездку и чаевые.'),
        ('cab','такси / машина','«Кабина» здесь обычно неверно.'),
        ('to hit the right one','угадать нужный','В переборе названий городов; не «нажать на нужный».'),
        ('to turn tail','развернуться / дать дёру','Идиома; не «повернуть хвост».'),
        ('Cruz','Крус','Аргентинская невеста Титуана; женский род. Телефонная собеседница Патрисии.'),
        ('Titouan','Титуан','Взрослый сын Патрисии. Лежит в коме после аварии.'),
        ('James','Джеймс','Австралиец, партнёр и будущий муж Пьеро.'),
        ('Karate Kid','Парень-каратист','Прозвище Пьеро и отсылка к фильму. Он занимался дзюдо; путаница намеренная.'),
        ('Paris Anti-Crime Squad','парижская бригада по борьбе с преступностью','Полицейское подразделение Пьеро; сотрудники работают в штатском.'),
        ('rehab / palliative care','реабилитационное отделение / паллиативная помощь','В рассказе Патрисии; rehab не означает лечение наркозависимости.'),
        ('Charles de Gaulle airport','аэропорт Шарль-де-Голль','Пурва встречает прилетающего, сама не улетает. Терминал 2C — сюжетная деталь.'),
        ('Saint-Julien-du-Pauvre Church','церковь Сен-Жюльен-ле-Повр','К ней едет Аларик. Название Saint-Michel — станция метро Сен-Мишель.'),
        ('Muslim mass','мусульманская месса','Намеренная детская ошибка Аларика; не исправлять механически на намаз.'),
    ]
    rows += [dict(category='term',english=e,russian=r,status='reviewed',note=n) for e,r,n in terms]
    rows += [dict(category='term',english=o['source_term']+' ('+o['owner']+')',russian=o['russian'],
        status='reviewed',note=o['notes']) for o in rules['objects']]
    rows += [dict(category='term',english=t['english'],russian=t['russian'],status='reviewed',note=t['notes'])
        for t in rules.get('terms',[]) if not any(x['category']=='term' and x['english']==t['english'] for x in rows)]
    write_json(ROOT/'review/glossary.json',rows)
    text = ['# Глоссарий Night Call', '', f'Редакция от {date.today():%d.%m.%Y}.', '',
        'Это рабочая терминологическая база. Статус «из исходного перевода» означает, что написание перенесено, но ещё требует сверки в диалогах; это не отметка о завершённой литературной редактуре.', '',
        '## Имена', '', '| Ключ | Оригинал | Русский вариант | Подтверждённый род | Проверка имени |', '|---|---|---|---|---|']
    for r in rows:
        if r['category']=='character':
            gender=r.get('gender_label') or {'male':'мужской','female':'женский','mixed':'девушка и парень'}.get(r['gender'],'ещё не закреплён')
            text.append('| '+r['key']+' | '+r['english'].replace('|','\\|')+' | '+r['russian']+' | '+gender+' | '+('проверено' if r['status']=='reviewed' else 'из исходного перевода')+' |')
    text += ['', '## Термины и названия', '', '| Оригинал | Русский вариант | Примечание |','|---|---|---|']
    for r in rows:
        if r['category']=='term':
            text.append('| '+r['english']+' | '+r['russian']+' | '+r['note']+' |')
    text += ['', '## Правила редактуры', '',
        '- Сверять текст по объекту, блоку и переходу ответа, а не по близости строк в выгрузке.',
        '- Не переводить идентификаторы, имена переменных, команды `$$ … $$` и адреса переходов.',
        '- Водитель — мужчина. Рассказчик обращается к игроку на «вы». К Круки водитель обращается на «ты».',
        '- Не менять «ты/вы» механически во всех диалогах: выбирать по отношениям персонажей и контексту оригинала.',
        '- Сохранять отрицания, степень уверенности, пол, временные отношения, говорящего и сюжетные факты.',
        '- Профессиональный и полицейский жаргон переводить в контексте. Избегать буквального перевода идиом.',
        '- Не приписывать персонажу чужую биографию. Досье привязывать к ключу `.bio`.',
        '- Несколько ответов могут вести в один блок. Каждый ответ должен оставаться самостоятельным.',
        '- Многоточия и паузы сохранять, когда они передают нерешительность. Не добавлять их автоматически.',
        '- Полную редактуру сохранять в `localization/reviewed/*.json`; генератор создаёт `review/full_review_changes.json` и обновляет `CHANGELOG_RU.md` вместе с исходными правками `review/changes.json`.',
        '- Структурная проверка и отсутствие английских букв не доказывают точность смысла или качество перевода.', '']
    if rules.get('external_text_rules'):
        text += ['', '## Пассидекс и титры', '',
            'Редакторские источники: `localization/passidex_reviewed.json` и `localization/credits_reviewed.json`. Французский основной; английский поясняет или служит источником общих титров без отдельной FR-версии. `work/reveals_ru.txt` — только совместимая выгрузка.', '',
            '| Правило | ID записей | Основание |', '|---|---|---|']
        for r in rules['external_text_rules']:
            text.append('| '+r['key']+' | '+', '.join(r['reveal_ids'])+' | '+r['reason']+' '+r['evidence']+' |')
    (ROOT/'GLOSSARY_RU.md').write_text('\n'.join(text),encoding='utf-8')
    print('glossary:',len(rows),'entries')

if __name__=='__main__': main()
