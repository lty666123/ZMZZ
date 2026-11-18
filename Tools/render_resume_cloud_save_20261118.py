from pathlib import Path
import importlib.util
import os
import sys
from pypdf import PdfReader

root = Path(r'C:\Users\ASUS\Documents\ChatGPT\Unity All\简历\投递终版_完整关键词版\云存档更新_20261118')
stem = '李天宇_Unity客户端实习简历_完整投递版'
pdf_path = root / f'{stem}.pdf'
pdf = PdfReader(pdf_path)
print('PDF pages:', len(pdf.pages))
texts = [page.extract_text() for page in pdf.pages]
assert '账号云存档' in ''.join(texts)
assert len(pdf.pages) <= 2, 'Unexpected extra page; adjust local paragraph spacing before delivery.'
for i, text in enumerate(texts):
    print('Page', i + 1, 'characters:', len(text), 'tail:', text[-90:])

dependencies = Path(sys.executable).parent.parent
os.environ['PATH'] = str(dependencies / 'native/poppler/Library/bin') + os.pathsep + os.environ.get('PATH', '')
skill = Path(r'C:\Users\ASUS\.codex\plugins\cache\openai-primary-runtime\documents\26.1112.11217\skills\documents')
spec = importlib.util.spec_from_file_location('resume_docx_renderer', skill / 'render_docx.py')
renderer = importlib.util.module_from_spec(spec)
spec.loader.exec_module(renderer)
# The Windows runtime has no bundled LibreOffice. Use the PDF exported from
# this exact staged DOCX by Word, then keep the packaged renderer's PNG QA path.
renderer.convert_to_pdf = lambda *args, **kwargs: (str(pdf_path), 'Native Word PDF export from the same DOCX')
sys.argv = ['render_docx.py', str(root / f'{stem}.docx'), '--output_dir', str(root / '排版检查'), '--dpi', '140']
renderer.main()
