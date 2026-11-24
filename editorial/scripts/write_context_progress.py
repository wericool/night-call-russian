from full_review import *
from datetime import date

def main():
    progress=read_json(DEST/'progress.json')
    sources=source_records()
    story=read_json(DEST/'story_validation.json')
    lines=['# Прогресс полной русификации Night Call', '',
        f'Рабочая редактура от {date.today():%d.%m.%Y}. Это список фактически проверенных сцен, а не план со статусом «готово».', '',
        f'Полные контекстные редакции сохранены для {progress["reviewed"]} из {progress["total"]} сцен. Это общий накопленный счётчик; ещё {progress["pending"]} не имеют полной редакции.', '',
        'С15.10.2026 пользователь выбрал французский текст основным смысловым источником. Исторические решения ниже нельзя автоматически считать окончательными по этому приоритету. Полная отдельная очередь: localization/french_priority_audit.json; правило: localization/source_priority.json. Английский остаётся вспомогательным, различия разбираются явно.', '',
        f'В проверенных сценах — блоков: {progress["reviewed_passages"]}; видимых строк: {progress["reviewed_visible_lines"]}; вариантов ответа: {progress["reviewed_choices"]}.', '',
        f'Отдельно восстановлено по французскому источнику: строк рассказчика — {progress.get("reviewed_additional_french_lines",0)}, вариантов ответа — {progress.get("reviewed_additional_french_choices",0)}. Эти добавления не включены в исторические счётчики английских слотов выше.', '',
        f'Позиции passengercall: проверено {progress["reviewed_passenger_calls"]} из {progress["total_passenger_calls"]}; локализуемых позиций сверено {progress["reviewed_translated_passenger_calls"]}, технических заглушек сохранено {progress["reviewed_technical_call_markers"]}. Включены также переведённый комментарий N/A у Сонни01 и молчаливое приглашение «…» Сонни02: это не два придуманных адреса. Данные хранятся в _user_datas отдельно от основных реплик и анимаций. NO PASSENGER CALL у Людивин — отдельная техническая позиция.', '',
        '## Что означает проверка', '',
        'В прежней редактуре прочитаны все реплики и варианты ответа, сверены смысл, говорящий, отношения, тон, условия веток и сюжетные факты. Сохранены оригинальные команды и переходы. Французский использовался для уточнений, но при явных различиях часто предпочитался английский. Новая сверка меняет этот приоритет; старые записи сохраняются как история решений, а не автоматическое исключение из нового правила. Это не равнозначно прохождению сцены в игре.', '',
        f'Проверка штатным Prompter.dll: '+('пройдена' if story['passed'] else 'НЕ ПРОЙДЕНА; запуск остановился с ошибкой')+f'. Успешных отдельных проверок: {story["checks"]}. Охват запуска: '+ '; '.join(story['scope'])+'. Результат, границы и хеши проверенного ввода — localization/story_validation.json. Для повторения сначала scripts/export_reviewed_engine.py, затем review/FullContextStoryChecks.exe ../translation/reviewed_engine_input.json; после изменения C# нужно пересобрать проверщик. Новый экспорт требует нового запуска проверок; старый результат нельзя переносить на изменённый ввод.', '',
        '## Контекстные решения', '']
    if (DEST/'french_priority_audit.json').exists():
        audit=read_json(DEST/'french_priority_audit.json')
        lines[7:7]=[f'По новому французскому приоритету приняты {audit["reviewed"]} из {audit["total"]} сцен; ключи интерфейса — {audit["ui_reviewed"]} из {audit["ui_total"]}. Это отдельная проверка, её не закрывает старый общий счётчик.', '']
    for scene in progress['scenes']:
        if scene['status']!='contextually_reviewed':continue
        r=read_json(REVIEWED/f'{scene["name"]}.json')
        lines += ['### '+scene['name'],'',r['context'],'']+['- '+s for s in r['decisions']]+['']
    lines += ['## Весь корпус', '', '| Сцена | Источник | Блоков | Строк | Ответов | Статус |', '|---|---|---:|---:|---:|---|']
    for scene in progress['scenes']:
        lines.append('| '+scene['name']+' | '+scene['kind']+' | '+str(scene['passages'])+' | '+str(scene['visible_lines'])+' | '+str(scene['choices'])+' | '+('полностью сверена' if scene['status']=='contextually_reviewed' else 'не сверена целиком')+' |')
    lines += ['', '## Продолжение работы', '',
        '1. Прочитать LOCALIZATION_CONTEXT_RU.md и контекст уже проверенных сюжетных сцен. Архив r1 не использовать как доказательство завершённой локализации.',
        '2. Сверять истории пассажиров целиком; повторные встречи читать одной связанной историей. Актуальную очередь брать из localization/progress.json, решения — из localization/consistency.json и localization/contexts/. Подтверждённые имя и пол не означают, что вся сцена отредактирована. Пол и игровые факты подтверждать исходными репликами, а не именами. Текущая точка продолжения записана в CONTINUE_HERE_RU.md.',
        '3. Полную сцену записывать через save_scene в scripts/full_review.py. Каждая исходная реплика и каждый ответ требуют явного русского варианта; проверка не допускает пропусков.',
        '3а. Переводить также все фразы passengercall из _user_datas: передать calls в save_scene либо записать в localization/passenger_calls.json. В этой таблице ключи — имена сцен и индексы исходных строк. Не менять префикс, анимации и технические маркеры N/A.',
        '4. Запускать scripts/full_review.py, scripts/check_consistency.py, scripts/corpus.py, scripts/write_glossary.py, scripts/write_context_progress.py и scripts/write_reports.py. Новые решения должны сохраняться в ../translation/reviewed, а не только в сгенерированном TXT.',
        '5. Для сценариев TextAsset проверять оригинал и перевод штатным PrompterCompiler.dll через review/ScriptChecks.exe. Он принимает два пути к файлам, не сами тексты.',
        '6. Не запускать scripts/author_review.py после контекстной редакторской стадии без последующего scripts/full_review.py: старый слой исходных правок может перезаписать отдельные рабочие тексты.',
        '7. После полного корпуса встроить также 36 сценариев TextAsset в патч. Сборщик r1 пока меняет 200 DialogObjectScript; это технический пункт, который ещё не выполнен.',
        '8. Не обновлять архив r1 и не объявлять финальный выпуск, пока полная редактура и проверка сборки не завершены.', '']
    (ROOT/'LOCALIZATION_PROGRESS_RU.md').write_text('\n'.join(lines),encoding='utf-8')
    print('Progress report saved.')
if __name__=='__main__':main()
