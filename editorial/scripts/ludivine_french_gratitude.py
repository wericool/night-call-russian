"""Exact existing FR-five/EN-four gratitude fold, for marker-slot validation only.

The accepted Russian scene already retains both speakers. No text or command is
rewritten here; adjacent narrative slots are counted according to that one
source-proven carrier alignment. Other scenes/passages retain their exact slots.
"""
ALIGNMENT = {'passage': 'ludivine-01-nextsteps-sheknows', 'french_lines': ['LUDIVINE : « Et quand je rentre, je détruis toutes les infos que j’ai sur vous… Ça vous va\xa0? »', 'Vous hochez la tête.', '« Merci… »', 'Ce mot résonne un moment dans le taxi. Votre cliente pose la main sur la portière.', 'LUDIVINE : « Merci à vous. »'], 'carrier_lines': ['LUDIVINE: “And then, when I get home, I’ll destroy all the information I have on you… Is that ok with you?”', 'You nod yes.', '“Thank you…”', 'This word echoes for a minute through the taxi. Your passenger reaches for the door.'], 'russian_lines': ['ЛЮДИВИН: «А дома уничтожу всё, что узнала о вас… Хорошо?»', 'Вы киваете.', '«Спасибо…»', 'Слово ещё некоторое время отзывается в салоне. Пассажирка кладёт руку на дверцу. «Это вам спасибо», — отвечает она.'], 'reason': 'FR5видимыхстрок/EN4: завершающее Merci à vous сохранено как прямая цитата пассажирки в последнем ENслоте после нарраторского эха благодарности/руки на дверце. Обе благодарности принадлежат разным говорящим, не слиты в ответ водителя. Технические действия/переход/числоENслотов не изменены.'}

def technical_lines(name, review, english, french):
    lines = french['_lines']
    if name != '054_ludivine_01' or english['_title'] != 'ludivine-01-nextsteps-sheknows':
        return lines
    assert french['_title'] == english['_title']
    assert english['_lines'] == ALIGNMENT['carrier_lines']
    assert lines == ALIGNMENT['french_lines']
    assert review.get('visible_text_alignment') == [ALIGNMENT]
    from corpus import command
    assert len(lines) == 5 and len(english['_lines']) == 4
    assert not any(command(s) for s in lines + english['_lines'])
    return lines[:3] + [' '.join(lines[3:])]
