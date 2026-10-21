from pathlib import Path
import re,sys
app=Path(sys.argv[1])
for folder in ('Views','Shared','Components'):
    for file in (app/folder).rglob('*'):
        if file.suffix not in ('.cs','.razor'): continue
        text=file.read_text(encoding='utf-8-sig')
        assert not re.search(r'PuntoAbaDbContext|Microsoft.EntityFrameworkCore|FileStream|File\\.Write|Directory\\.Create',text), f'Persistencia en interfaz: {file}'
print('OK: Las vistas y componentes no acceden directamente a SQLite ni escriben archivos.')
