import requests
import sqlite3
import os
import json
import time

os.chdir(os.path.dirname(__file__))

# 单韵母a o e i u ü
yunmu = {
    'a': ('a', 'ā', 'á', 'ǎ', 'à'),
    'o': ('o', 'ō', 'ó', 'ǒ', 'ò'),
    'e': ('e', 'ē', 'é', 'ě', 'è'),
    'i': ('i','ī', 'í', 'ǐ', 'ì'),
    'u': ('u', 'ū', 'ú', 'ǔ', 'ù'),
    'v': ('ü', 'ǖ', 'ǘ', 'ǚ', 'ǜ'),
}

def name_to_py_spell(name: str):
    tone = name[-1].isdecimal() and int(name[-1]) or 0
    if tone > 0:
        name = name[0:-1]
    spell = []
    danyunmu = list(yunmu.keys())
    yun_pos = len(danyunmu)
    tone_pos = -1
    for i in range(len(name)):
        c = name[i]
        if c in yunmu:
            spell.append(yunmu[c][0])
            if tone > 0 and danyunmu.index(c) < yun_pos:
                yun_pos = danyunmu.index(c)
                tone_pos = i
        else:
            spell.append(c)
    if tone_pos >= 0:
        spell[tone_pos] = yunmu[name[tone_pos]][tone]
    return ''.join(spell)

def open_db():
    return sqlite3.connect('kindergarten.db')

# in sqlite, all char will be convert to text
with open_db() as conn:
    conn.execute(
    '''
        CREATE TABLE IF NOT EXISTS pinyin(
        id INTEGER PRIMARY KEY AUTOINCREMENT,
        name TEXT UNIQUE NOT NULL,
        src TEXT NOT NULL,
        yin TEXT NOT NULL,
        type TEXT NOT NULL,
        spell TEXT NOT NULL
        )
    '''
    )

def upsert(value_map: dict):
    keys = value_map.keys()
    values = [repr(value_map[k]) for k in keys]
    sql = f'''
    INSERT INTO pinyin({','.join(keys)}) VALUES({','.join(values)}) 
    ON CONFLICT(name) DO NOTHING
    '''
    print(sql)
    with open_db() as conn:
        conn.execute(sql)

# 网站页面链接
url_base = 'http://xxxx1.com'
data = "/json/url2.json"

def download_assets(src: str):
    if os.path.isfile(src):
        print('asset exist: ' + src)
        return

    directory = os.path.dirname(src)
    if not os.path.isdir(directory):
        os.makedirs(directory)

    with requests.get(url_base + src[1:], stream=True) as r:
        r.raise_for_status()
        with open(src, 'wb') as f:
            for chunk in r.iter_content(chunk_size=8192):
                f.write(chunk)

# 爬取网页内容并解析
response = requests.get(url_base + data)
response.raise_for_status()

jo = json.loads(response.content)
for dd in jo['data'].values():
    for d in dd:
        if d['type'] == '汉语拼音字母全表':
            continue
        
        d['spell'] = name_to_py_spell(d['name'])
        upsert(d)
        download_assets(d['src'])
        download_assets('./picture/' + d['name'] + '.jpg')
        time.sleep(0.1)