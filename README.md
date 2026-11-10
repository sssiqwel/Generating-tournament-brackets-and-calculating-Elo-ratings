# Турниры и рейтинги

[![CI](https://github.com/sssiqwel/Generating-tournament-brackets-and-calculating-Elo-ratings/actions/workflows/ci.yml/badge.svg)](https://github.com/sssiqwel/Generating-tournament-brackets-and-calculating-Elo-ratings/actions/workflows/ci.yml)

Учебный проект: генерация турнирных сеток и расчёт рейтинга Эло.

## Проверка

В терминале из папки проекта выполните:

    dotnet test Tournaments.slnx


## Что сделано

- Вход игрока: пароль хранится хешем, в ответ приходит JWT.
- Турнир: участники, круговая, олимпийская и швейцарская сетка, запись результата и перенос победителя.
- Сообщение о завершённом матче в RabbitMQ и пересчёт Эло.
- Сайт: вход, список турниров, страница турнира, запись счёта и таблица лидеров.

Регистрация есть в сервисе входа. Отдельной страницы регистрации на сайте нет.

## Сайт

Нужны пять программ, каждая в своём терминале:

    dotnet run --project src/Identity.Api
    dotnet run --project src/Tournaments.Api
    dotnet run --project src/Rating.Api
    dotnet run --project src/Gateway
    dotnet run --project src/Tournaments.Web

Сайт: http://localhost:5090

Проверочный вход: почта `player2@mail.test`, пароль `password1`.

# Docker

## Запуск

 Docker подключается командой `colima start`. Из папки проекта:

    docker compose up --build

Проверки:

    http://localhost:8080/health
    http://localhost:8080/identity/health
    http://localhost:8080/tournaments/health
    http://localhost:8080/rating/health

Очередь: http://localhost:15672, имя и пароль `tournament`.
Остановка: `docker compose down`