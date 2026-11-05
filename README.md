# Турниры и рейтинги

[![CI](https://github.com/sssiqwel/Generating-tournament-brackets-and-calculating-Elo-ratings/actions/workflows/ci.yml/badge.svg)](https://github.com/sssiqwel/Generating-tournament-brackets-and-calculating-Elo-ratings/actions/workflows/ci.yml)

Учебный проект: генерация турнирных сеток и расчёт рейтинга Эло.

## Проверка

В терминале из папки проекта выполните:

    dotnet test Tournaments.slnx

# Docker

## Запуск

На этом компьютере Docker включается командой `colima start`. Из папки проекта:

    docker compose up --build

Проверки:

    http://localhost:8080/health
    http://localhost:8080/identity/health
    http://localhost:8080/tournaments/health
    http://localhost:8080/rating/health

Очередь: http://localhost:15672, имя и пароль `tournament`.
Остановка: `docker compose down`