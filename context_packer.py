import os

# Какие расширения файлов нам интересны
INTERESTING_EXTENSIONS = {'.py', '.cs', '.axaml', '.xaml', '.md', '.json', '.xml'}

# Какие папки мы ЖЕСТКО игнорируем
IGNORE_DIRS = {
    '.git', '.vs', '.idea', '.vscode',
    '__pycache__', '.venv', 'venv', 'env',
    'bin', 'obj', 'Debug', 'Release',
    'assets', 'media'  # Игнорируем тяжелые бинарники
}

# Какие файлы игнорируем
IGNORE_FILES = {
    'package-lock.json', 'context_packer.py', 'FULL_CONTEXT.txt', 'dance_data.json'
}


def is_ignored(path, names):
    return {name for name in names if name in IGNORE_DIRS}


def pack_project():
    output_file = 'FULL_CONTEXT.txt'

    with open(output_file, 'w', encoding='utf-8') as outfile:
        # Пишем заголовок
        outfile.write("# PROJECT SNAPSHOT\n")
        outfile.write("# ==========================================\n\n")

        # Обходим дерево
        for root, dirs, files in os.walk('.'):
            # Фильтрация папок
            dirs[:] = [d for d in dirs if d not in IGNORE_DIRS]

            for file in files:
                if file in IGNORE_FILES: continue

                _, ext = os.path.splitext(file)
                if ext not in INTERESTING_EXTENSIONS: continue

                file_path = os.path.join(root, file)

                # Записываем путь файла
                outfile.write(f"\n# --- FILE: {file_path} ---\n")

                try:
                    with open(file_path, 'r', encoding='utf-8') as infile:
                        content = infile.read()
                        outfile.write(content)
                        outfile.write("\n")
                except Exception as e:
                    outfile.write(f"# Error reading file: {e}\n")

    print(f"Готово! Весь проект собран в файл: {output_file}")
    print(f"Размер: {os.path.getsize(output_file) / 1024:.2f} KB")


if __name__ == '__main__':
    pack_project()
