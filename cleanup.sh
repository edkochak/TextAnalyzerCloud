#!/bin/bash

# Цвета для вывода
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

echo -e "${YELLOW}Очистка проекта перед отправкой в Git...${NC}"

# Удаление bin и obj папок
find . -type d -name bin -o -name obj | xargs -I {} rm -rf "{}"
echo -e "${GREEN}✓${NC} Удалены папки bin и obj"

# Удаление всех файлов .user
find . -name "*.user" -type f -delete
echo -e "${GREEN}✓${NC} Удалены файлы .user"

# Удаление тестовых результатов
find . -type d -name TestResults -o -name .coverage | xargs -I {} rm -rf "{}"
echo -e "${GREEN}✓${NC} Удалены тестовые результаты"

# Создаем .gitkeep в пустых папках StoredFiles
find . -name "StoredFiles" -type d | while read dir; do
    if [ -z "$(ls -A "$dir")" ]; then
        touch "$dir/.gitkeep"
        echo -e "${GREEN}✓${NC} Добавлен .gitkeep в $dir"
    fi
done

# Удаление Mac OS файлов
find . -name ".DS_Store" -o -name "._*" -type f -delete
echo -e "${GREEN}✓${NC} Удалены служебные файлы Mac OS"

# Оставляем в StoredFiles только .gitkeep
find . -path "*/StoredFiles/*" -type f ! -name ".gitkeep" -delete
echo -e "${GREEN}✓${NC} Очищены загруженные файлы в StoredFiles"

# Удаление файлов с расширениями образов и временных файлов в корне
rm -f *.png *.svg *.jpg *.jpeg 2>/dev/null
echo -e "${GREEN}✓${NC} Удалены временные файлы изображений"

# Удаление бинарных файлов в корне (загруженные файлы)
rm -f [0-9a-f][0-9a-f][0-9a-f][0-9a-f][0-9a-f][0-9a-f][0-9a-f][0-9a-f]-[0-9a-f][0-9a-f][0-9a-f][0-9a-f]-[0-9a-f][0-9a-f][0-9a-f][0-9a-f]-[0-9a-f][0-9a-f][0-9a-f][0-9a-f]-[0-9a-f][0-9a-f][0-9a-f][0-9a-f][0-9a-f][0-9a-f][0-9a-f][0-9a-f][0-9a-f][0-9a-f][0-9a-f][0-9a-f] 2>/dev/null
echo -e "${GREEN}✓${NC} Удалены загруженные файлы в корне (по шаблону UUID)"

# Создаем git init, если не инициализирован
if [ ! -d ".git" ]; then
    git init
    echo -e "${GREEN}✓${NC} Инициализирован Git репозиторий"
fi

echo -e "${YELLOW}Список файлов, которые будут добавлены в Git:${NC}"
git add -A -n

echo -e "\n${GREEN}Очистка завершена!${NC}"
echo -e "Для добавления файлов в Git выполните: ${YELLOW}git add .${NC}"
echo -e "Для коммита выполните: ${YELLOW}git commit -m \"Initial commit\"${NC}"
echo -e "Для указания удаленного репозитория: ${YELLOW}git remote add origin <url-репозитория>${NC}"
echo -e "Для отправки в репозиторий: ${YELLOW}git push -u origin main${NC}"
