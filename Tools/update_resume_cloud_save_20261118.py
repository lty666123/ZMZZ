from copy import deepcopy
from pathlib import Path
import re
import shutil
from docx import Document
from docx.text.paragraph import Paragraph

root = Path(r'C:\Users\ASUS\Documents\ChatGPT\Unity All\简历\投递终版_完整关键词版')
stem = '李天宇_Unity客户端实习简历_完整投递版'
work = root / '云存档更新_20261118'
backup = work / '原版备份'
work.mkdir(exist_ok=True)
backup.mkdir(exist_ok=True)
for ext in ['md', 'docx', 'pdf']:
    original = root / f'{stem}.{ext}'
    target = backup / original.name
    if not target.exists():
        shutil.copy2(original, target)

source_md = (root / f'{stem}.md').read_text(encoding='utf-8')
old_skill = next(line for line in source_md.splitlines() if line.startswith('- **配置与数据持久化：**'))
new_skill = '- **配置与数据持久化：**使用 ScriptableObject 管理静态配置，通过物品 ID 和字典索引关联运行数据；使用 JsonUtility、LitJson 和文件读写实现本地保存，结合 ASP.NET Core 与 SQLite 实现账号云存档，具备登录后读档、事件驱动上传及失败重试实践。'
cloud_bullet = '- **账号云存档：**使用 UnityWebRequest 对接 ASP.NET Core 与 SQLite，按登录会话中的 PlayerId 保存玩家、背包、装备及世界进度；读档校验后重建运行时对象，通过事件合并保存请求、串行上传并失败重试，保留上传期间的新变化；验证重新登录、服务器重启后进度恢复及账号隔离。'
ui_line = next(line for line in source_md.splitlines() if line.startswith('- **UI 与数据保存：**'))
assert '**账号云存档：**' not in source_md
updated_md = source_md.replace(old_skill, new_skill, 1).replace(ui_line, ui_line + '\n' + cloud_bullet, 1)
(work / f'{stem}.md').write_text(updated_md, encoding='utf-8')

doc = Document(root / f'{stem}.docx')
before = [p.text for p in doc.paragraphs]
def rich_replace(p, markdown):
    old_properties = deepcopy(p.runs[0]._r.rPr) if p.runs and p.runs[0]._r.rPr is not None else None
    p.clear()
    for index, part in enumerate(re.split(r'\*\*(.*?)\*\*', markdown)):
        if not part:
            continue
        run = p.add_run(part)
        if old_properties is not None:
            run._r.insert(0, deepcopy(old_properties))
        run.bold = bool(index % 2)

skill_paragraph = next(p for p in doc.paragraphs if p.text.startswith('配置与数据持久化：'))
rich_replace(skill_paragraph, new_skill[2:])
ui_paragraph = next(p for p in doc.paragraphs if p.text.startswith('UI 与数据保存：'))
new_element = deepcopy(ui_paragraph._p)
ui_paragraph._p.addnext(new_element)
new_paragraph = Paragraph(new_element, ui_paragraph._parent)
rich_replace(new_paragraph, cloud_bullet[2:])
output = work / f'{stem}.docx'
doc.save(output)
reopened = Document(output)
texts = [p.text for p in reopened.paragraphs]
assert len(texts) == len(before) + 1
assert sum(t.startswith('账号云存档：') for t in texts) == 1
assert all(texts.count(t) == before.count(t) for t in before if not t.startswith('配置与数据持久化：'))
assert '账号云存档：' in ''.join(texts)
assert len(reopened.sections) == len(doc.sections)
print('Staged MD and DOCX; original versions backed up; only one skill paragraph changed and one project bullet added.')
print(output)
