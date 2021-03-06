﻿# Использование FTS в PostgreSQL
#### 1. Тип tsvector
Выполните запрос
```sql
SELECT to_tsvector('The quick brown fox jumped over the lazy dog.');
```
В ответ будет возвращён список [лексем](https://en.wikipedia.org/wiki/Lexeme)
```sql
                to_tsvector
'brown':3 'dog':9 'fox':4 'jump':5 'lazi':8 'quick':2
```

> Задание 1
1. Изучите документацию к функции `to_tsvector`
2. Вызовите эту функцию для следующей строки: `Съешь ещё этих мягких французских булок, да выпей чаю`
3. Почему в векторе нет слова `да`?

Это стоп-слово, которое часто появляется в каждом документе и поэтому не имеет различительной ценности, соответственно в векторе его нет.
#### 2. Тип tsquery
Выполните по очереди запросы
```sql
--№1
SELECT to_tsvector('The quick brown fox jumped over the lazy dog')  
    @@ to_tsquery('fox');
--№2
SELECT to_tsvector('The quick brown fox jumped over the lazy dog')  
    @@ to_tsquery('foxes');
--№3 
SELECT to_tsvector('The quick brown fox jumped over the lazy dog')  
    @@ to_tsquery('foxhound');
```

> Задание 2
1. Что означают символы `@@`

tsvector соответствует tsquery ?

2. Почему второй запрос возвращает `true`, а третий не возвращает

Для fox и foxes лексема {fox}, для foxhound лексема {foxhound}. Из-за разных лексем, третий возвращает `false`.
  
3. Выполните запрос
```sql
SELECT to_tsvector('Russian', 'Съешь ещё этих мягких французских булок, да выпей чаю.')
    @@ to_tsquery('Russian','булка');
```
Почему слово булка не находится?

В словаре для слова булка содержится лексема {булк}, для булок лексема {булок}.
Поэтому при данном запросе возвращается `false`.

4. Используйте функцию `select ts_lexize('russian_stem', 'булок');` для того чтобы понять почему.
5. Замените в предложении слово `булок`, на слово `пирожков`
Выполните запросы
```sql
--№1
SELECT to_tsvector('Russian', 'Съешь ещё этих мягких французских пирожков, да выпей чаю.')
    @@ to_tsquery('Russian','пирожки');
--№2
SELECT to_tsvector('Russian', 'Съешь ещё этих мягких французских пирожков, да выпей чаю.')
    @@ to_tsquery('Russian','пирожок');
```
Почему первый запрос возвращает `true`, а второй не возвращает?

Пирожков и пирожки имеют одинаковую лексему {пирожк}, тогда как пирожок имеет лексему {пирожок}.Поэтому при втором запросе возвращается `false`.
#### 3. Операторы
Выполните запрос
```sql
--И
SELECT to_tsvector('The quick brown fox jumped over the lazy dog')  
    @@ to_tsquery('fox & dog');

--ИЛИ
SELECT to_tsvector('The quick brown fox jumped over the lazy dog')  
    @@ to_tsquery('fox | rat');

--отрицание
SELECT to_tsvector('The quick brown fox jumped over the lazy dog')  
    @@ to_tsquery('!clown');

--группировка
SELECT to_tsvector('The quick brown fox jumped over the lazy dog')  
    @@ to_tsquery('fox & (dog | rat) & !mice');
```
> Задание 3
1. Приведите аналогичные запросы для любого предложения на русском
  ```sql
--И
SELECT to_tsvector('Russian', 'Съешь ещё этих мягких французских пирожков, да выпей чаю.')  
    @@ to_tsquery('Russian', 'пирожки & чай');

--ИЛИ
SELECT to_tsvector('Russian', 'Съешь ещё этих мягких французских пирожков, да выпей чаю.')  
    @@ to_tsquery('Russian', 'пирожок | чай');

--отрицание
SELECT to_tsvector('Russian', 'Съешь ещё этих мягких французских пирожков, да выпей чаю.')  
    @@ to_tsquery('Russian', '!Река');

--группировка
SELECT to_tsvector('Russian', 'Съешь ещё этих мягких французских пирожков, да выпей чаю.')  
    @@ to_tsquery('Russian', 'пирожки & (чай | река) & !лес');
```
2. Почему для английского языка не нужно указывать язык в первом аргументе и какой анализатор используется если никакой не указан?

Для английского необязательно указывать язык, т.к. при отсутствии этого параметра, используется default configuration в котором используется английский.
Анализатор pg_catalog.default.
#### 4. Поиск фраз
Изучите документацию по [операторам](https://www.postgresql.org/docs/current/functions-textsearch.html) FTS
Выполните запрос
```sql

SELECT to_tsvector('Russian', 'Съешь ещё этих мягких французских булок, да выпей чаю.')
    @@ to_tsquery('Russian','мягких<2>булок');
```
> Задание 4
1. Что означает число 2 в операторе `<->`

`<2>` допускает существование ровно одной лексемы между этими лексемами, тогда как при `<->` лексемы должны находится рядом.

2. Модифицируйте запрос так, чтобы можно было найти фразу `съешь ещё`
```sql
SELECT to_tsvector('Russian', 'Съешь ещё этих мягких французских булок, да выпей чаю.')
    @@ to_tsquery('Russian','съешь<->ещё');
```
3. Для чего нужно использовать функцию `phraseto_tsquery`
Функция `phraseto_tsquery` задействует оператор `<->` для конструирования `tsquery`, который может содержать многословную фразу, включающую в себя стоп-слова.
#### 5. Утилиты
1. Приведите примеры использования функций `ts_debug` и  `ts_headline`
```sql
--ts_debug
SELECT * from ts_debug('Russian','Съешь ещё этих мягких французских булок, да выпей чаю.');

--ts_headline
SELECT ts_headline('Russian', 'Съешь ещё этих мягких французских булок, да выпей чаю.', to_tsquery('мягкий & чай'),
  'StartSel = <, StopSel = >');
```
