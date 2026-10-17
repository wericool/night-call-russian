"""Post-process a built native payload: Passidex reveals, credits roles, startup warning.

Usage: patch_extra_text.py <payload Night Call_Data dir> [--dry]
Reads game sources from the installed (pristine) game for the extra scene files and
overwrites/creates patched copies in the payload directory.
"""
import sys, re, json, hashlib, copy
from pathlib import Path
import UnityPy
from UnityPy.helpers.TypeTreeGenerator import TypeTreeGenerator
from extract_original import GAME, corrected_node

ROOT = Path(__file__).resolve().parents[1]
ROLES = {
 '3D advice': 'Консультации по 3D', 'Art & Background': 'Графика и фоны', 'Art & Characters': 'Графика и персонажи',
 'UI & UX': 'Интерфейс и UX', 'Chief Operating Officer': 'Операционный директор', 'Code': 'Программирование',
 'Code & Port': 'Программирование и портирование', 'Code & Production': 'Программирование и продюсирование',
 'Code & QA': 'Программирование и тестирование', 'Code & Tools': 'Программирование и инструменты',
 'Design & Production': 'Дизайн и продюсирование', 'Emotional guidance': 'Эмоциональная поддержка',
 'English Proofreading': 'Корректура английского текста', 'English Translation': 'Перевод на английский',
 'European Support': 'Поддержка в Европе', 'Ex Partner in Crime': 'Бывший подельник',
 'For all these adventures together.': 'За все наши совместные приключения.',
 'For all these years of friendship': 'За все эти годы дружбы',
 'For being unaware of entrusting me with the interfaces of his projects': 'За то, что и не подозревал, что доверил мне интерфейсы своих проектов',
 'For everything': 'За всё', 'For his precious guidance': 'За бесценные советы', 'For your support': 'За вашу поддержку',
 'French PR': 'Связи с общественностью (Франция)', 'French Proofreading': 'Корректура французского текста',
 'GIF God': 'Бог гифок', 'GIS advice': 'Консультации по ГИС', 'Gameplay & Tools': 'Геймплей и инструменты',
 'German Localization': 'Немецкая локализация', 'Guest Writer': 'Приглашённый сценарист',
 'Japanese Localization (Shinji)': 'Японская локализация (Синдзи)', 'Legal': 'Юридическая поддержка', 'Localization Coordinator': 'Координатор локализации',
 'Logo': 'Логотип', 'Long-time Support': 'Поддержка на протяжении многих лет', 'Love & Food': 'Любовь и еда',
 'Music & Sound': 'Музыка и звук', 'My Everything': 'Моё всё', 'My Player 2': 'Мой второй игрок', 'My brothers': 'Мои братья',
 'My heroes of everyday life': 'Мои герои повседневной жизни', 'Producer': 'Продюсер', 'Programmer': 'Программист',
 'Providing Daily Doses of Happiness': 'Ежедневные дозы счастья', 'Server savior': 'Спаситель серверов',
 'Special Thanks': 'Особая благодарность', 'Support & Food': 'Поддержка и еда', 'Support & Love': 'Поддержка и любовь',
 'Support & Love & Princess': 'Поддержка, любовь и принцесса', 'Support & Pizza': 'Поддержка и пицца',
 'Thanks for the help, support and nice words': 'Спасибо за помощь, поддержку и добрые слова',
 'Thanks for the love & support! You rock!': 'Спасибо за любовь и поддержку! Вы лучшие!',
 'Toonboom/Harmony support': 'Поддержка по Toon Boom Harmony', 'Unity Support': 'Поддержка по Unity',
 'Unity, Support & bad Overwatch ranking': 'Unity, поддержка и плохой рейтинг в Overwatch',
 'Who gave me in secret my first video games and comics that brought me in this beautiful job': 'Тайком подарила мне первые видеоигры и комиксы, которые привели меня в эту прекрасную профессию',
 'Writing & Design': 'Сценарий и дизайн',
}
HEADS = {'Core Team': 'Основная команда', 'Additional Team': 'Дополнительная команда', 'Team Pets': 'Питомцы команды',
 'Interns': 'Стажёры', 'Loc Team': 'Команда локализации', 'Additional Writing': 'Дополнительные тексты',
 'Special Thanks': 'Особая благодарность', 'Development Team': 'Команда разработки', 'Production Baby': 'Продюсерский малыш'}
NAMES = {'Family & friends': 'Семья и друзья', 'My "bêta-testeurs"': 'Мои «бета-тестеры»'}
SPLASH = {
 'Night Call is inspired by real life stories and therefore may disturb some players.': 'Night Call вдохновлена реальными историями и может шокировать некоторых игроков.',
 'Do not turn off the game or your computer when this symbol is on screen.': 'Не выходите из игры и не выключайте компьютер, пока этот символ отображается на экране.',
}
unmatched = set()

def tr_credit(t):
    def rep(m):
        r = m.group(1)
        if r in ROLES: return '<size=30><i>— ' + ROLES[r] + '</i></size>'
        unmatched.add(r); return m.group(0)
    o = re.sub(r'<size=30><i>— (.*?)</i></size>', rep, t)
    o = re.sub(r'<u>(.*?)</u>', lambda m: '<u>' + HEADS.get(m.group(1), m.group(1)) + '</u>', o)
    o = re.sub(r'<b>([^<]*)</b> — (.*)$', lambda m: '<b>' + m.group(1) + '</b> — ' + ROLES.get(m.group(2), m.group(2)), o)
    for k, v in NAMES.items():
        if o.startswith(k): o = v + o[len(k):]
    return o

def sha(p): return hashlib.sha256(Path(p).read_bytes()).hexdigest()

def main():
    payload = Path(sys.argv[1]); dry = '--dry' in sys.argv
    outdir = Path(sys.argv[2]) if len(sys.argv) > 2 and not sys.argv[2].startswith('--') else None
    if outdir: outdir.mkdir(parents=True, exist_ok=True)
    reveals = {}
    for l in (ROOT / 'work/reveals_ru.txt').read_text(encoding='utf-8').split('\n'):
        if l.strip():
            i, t = l.split('|', 1); reveals[int(i)] = t
    assert sorted(reveals) == list(range(481))
    gen = TypeTreeGenerator('2018.4.0f1'); gen.load_local_dll_folder(str(GAME / 'Managed'))
    report = []

    def open_asset(name, src_dir):
        env = UnityPy.load(str(src_dir / name)); env.typetree_generator = gen
        primary = next(a for a in env.assets if a.name == name)
        return env, primary

    # 1. sharedassets0 from payload: reveals + extra locale key
    env, pr = open_asset('sharedassets0.assets', payload)
    before = {o.path_id: o.get_raw_data() for o in pr.objects.values()}
    nrev = 0
    for obj in pr.objects.values():
        if obj.type.name != 'MonoBehaviour': continue
        cls = obj.parse_monobehaviour_head().m_Script.deref_parse_as_object().m_ClassName
        if cls == 'RevealScript':
            node = corrected_node(obj.generate_monobehaviour_node()); d = obj.read_typetree(nodes=node)
            assert len(d['reveals']) == 481
            for i, r in enumerate(d['reveals']):
                assert r['text'][1] == r['text'][2]
                r['text'][1] = reveals[i]; r['text'][2] = reveals[i]; nrev += 1
            if not dry: obj.save_typetree(d, nodes=node)
        elif cls == 'LocalizationData':
            node = corrected_node(obj.generate_monobehaviour_node()); d = obj.read_typetree(nodes=node)
            if d['m_Name'] == 'LocalizationDataENG' and not any(x['_key'] == 'DISPLAY MODE' for x in d['_items']):
                item = copy.deepcopy(d['_items'][0]); item['_key'] = 'DISPLAY MODE'; item['_value'] = 'Режим экрана'
                d['_items'].append(item)
                if not dry: obj.save_typetree(d, nodes=node)
    report.append(('sharedassets0.assets', nrev, 'reveals'))
    if not dry:
        out = outdir / 'sharedassets0.assets'; out.write_bytes(pr.save())
        chk = UnityPy.load(str(out)); sv = chk.assets[0]
        changed = [p for p, d in before.items() if sv.objects[p].get_raw_data() != d]
        assert set(sv.objects) == set(before) and len(changed) <= 4, changed

    # 2. scene/assets files from the pristine game: splash + credits
    extra = []
    for name, mode in [('level1', 'splash'), ('level3', 'credits'), ('level7', 'credits'), ('sharedassets3.assets', 'credits')]:
        env, pr = open_asset(name, GAME)
        before = {o.path_id: o.get_raw_data() for o in pr.objects.values()}
        n = 0
        for obj in pr.objects.values():
            if obj.type.name != 'MonoBehaviour': continue
            try: d = obj.read_typetree()
            except Exception: continue
            t = d.get('m_text')
            if not isinstance(t, str): continue
            new = SPLASH.get(t) if mode == 'splash' else (tr_credit(t) if mode == 'credits' else t)
            if new and new != t:
                d['m_text'] = new; n += 1
                if not dry: obj.save_typetree(d)
        report.append((name, n, mode))
        if n and not dry:
            out = outdir / name; out.write_bytes(pr.save())
            chk = UnityPy.load(str(out)); sv = chk.assets[0]
            ch = [p for p, dd in before.items() if sv.objects[p].get_raw_data() != dd]
            assert set(sv.objects) == set(before) and len(ch) == n, (name, len(ch), n)
            extra.append(name)
    print(report); print('unmatched roles:', sorted(unmatched))
    if not dry:
        json.dump(dict(extra_files=extra, report=report), open(ROOT / 'review/patch_extra_text.json', 'w', encoding='utf-8'), ensure_ascii=False, indent=1)

if __name__ == '__main__':
    main()
