from pathlib import Path
import hashlib
import importlib.util
import os
import sys
import zipfile

from docx import Document
from pypdf import PdfReader

ROOT = Path(r'C:\Users\ASUS\Documents\ChatGPT\Unity All\简历\投递终版_完整关键词版')
SOURCE = '李天宇_Unity客户端开发实习_简历'
TARGET = '李天宇_Unity客户端开发_简历'
SKILL = Path(r'C:\Users\ASUS\.codex\plugins\cache\openai-primary-runtime\documents\26.1112.11217\skills\documents')
REPLACEMENTS = {
    '求职意向：Unity 客户端开发实习生': '求职意向：Unity 客户端开发',
    '可到岗：2026.12.01｜每周可出勤：7 天｜可连续实习：至少 3 个月': '可到岗：2026.12.01',
    'Unity 客户端开发实习': 'Unity 客户端开发',
}


def edited(text):
    for old, new in REPLACEMENTS.items():
        text = text.replace(old, new)
    return text


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def build():
    source_hashes = {ext: digest(ROOT / f'{SOURCE}.{ext}') for ext in ('md', 'docx', 'pdf')}
    for ext in ('md', 'docx', 'pdf'):
        assert not (ROOT / f'{TARGET}.{ext}').exists(), f'Target already exists: {ext}'

    markdown = (ROOT / f'{SOURCE}.md').read_text(encoding='utf-8')
    new_markdown = edited(markdown)
    assert new_markdown != markdown
    assert '实习' not in new_markdown

    doc = Document(ROOT / f'{SOURCE}.docx')
    original_body = [p.text for p in doc.paragraphs]
    elements = [doc.element]
    for section in doc.sections:
        elements.extend([section.header._element, section.footer._element])
    seen = set()
    for element in elements:
        if id(element) in seen:
            continue
        seen.add(id(element))
        for text_node in element.xpath('.//w:t'):
            if text_node.text:
                text_node.text = edited(text_node.text)

    props = doc.core_properties
    for field in ('title', 'subject', 'keywords', 'comments', 'category'):
        value = getattr(props, field)
        if value:
            setattr(props, field, value.replace('实习生', '').replace('实习', ''))

    output_docx = ROOT / f'{TARGET}.docx'
    doc.save(output_docx)
    (ROOT / f'{TARGET}.md').write_text(new_markdown, encoding='utf-8')

    reopened = Document(output_docx)
    assert [p.text for p in reopened.paragraphs] == [edited(text) for text in original_body]
    assert '账号云存档' in ''.join(p.text for p in reopened.paragraphs)
    with zipfile.ZipFile(output_docx) as archive:
        for name in archive.namelist():
            if name.endswith('.xml'):
                assert '实习' not in archive.read(name).decode('utf-8'), name
    assert source_hashes == {ext: digest(ROOT / f'{SOURCE}.{ext}') for ext in source_hashes}
    print('Created separate DOCX and Markdown; original files unchanged; all internship wording removed.')


def render_and_verify():
    pdf_path = ROOT / f'{TARGET}.pdf'
    pdf = PdfReader(pdf_path)
    texts = [page.extract_text() or '' for page in pdf.pages]
    assert len(texts) == 2, f'Unexpected page count: {len(texts)}'
    assert '实习' not in ''.join(texts)
    assert '账号云存档' in ''.join(texts)
    assert '实习' not in str(pdf.metadata)
    for index, text in enumerate(texts, 1):
        print(f'Page {index}: {len(text)} characters')

    dependencies = Path(sys.executable).parent.parent
    os.environ['PATH'] = str(dependencies / 'native/poppler/Library/bin') + os.pathsep + os.environ.get('PATH', '')
    spec = importlib.util.spec_from_file_location('resume_variant_renderer', SKILL / 'render_docx.py')
    renderer = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(renderer)
    renderer.convert_to_pdf = lambda *args, **kwargs: (str(pdf_path), 'Native Word export of this exact DOCX')
    sys.argv = ['render_docx.py', str(ROOT / f'{TARGET}.docx'), '--output_dir', str(ROOT / 'qa' / '去实习版_20261118'), '--dpi', '140']
    renderer.main()
    print('Verified two pages and absence of internship wording; rendered every page for visual inspection.')


if __name__ == '__main__':
    {'build': build, 'verify': render_and_verify}[sys.argv[1]]()
