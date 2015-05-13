SELECT
    System.Timestamp as WindowEnd,
    login, COUNT(*)
FROM
    [errologauth]
TIMESTAMP BY 
    EventTime
WHERE
    [failed] = 1
GROUP BY TUMBLINGWINDOW(mi, 5), login
HAVING COUNT(*) > 3