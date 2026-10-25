from pathlib import Path
import sys
import zipfile
from lxml import etree

source = Path(sys.argv[1])
target = Path(sys.argv[2])
lines = []
with zipfile.ZipFile(source) as package:
    root = etree.fromstring(package.read("word/document.xml"))
ns = {"w": "http://schemas.openxmlformats.org/wordprocessingml/2006/main"}
table_index = 0
for element in root.xpath("//w:body/*", namespaces=ns):
    if element.tag.endswith("}p"):
        text = "".join(element.xpath(".//w:t/text()", namespaces=ns)).strip()
        if text:
            styles = element.xpath("./w:pPr/w:pStyle/@w:val", namespaces=ns)
            lines.append(f"[{styles[0] if styles else 'Paragraph'}] {text}")
    elif element.tag.endswith("}tbl"):
        table_index += 1
        lines.append(f"[TABLE {table_index}]")
        for row in element.xpath("./w:tr", namespaces=ns):
            cells = []
            for cell in row.xpath("./w:tc", namespaces=ns):
                parts = [value.strip() for value in cell.xpath(".//w:t/text()", namespaces=ns) if value.strip()]
                cells.append(" ".join(parts))
            lines.append(" | ".join(cells))
target.write_text("\n".join(lines), encoding="utf-8")
print(f"tables={table_index} lines={len(lines)}")
