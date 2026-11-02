USE ApexPostDb;
GO

-- Shipment volume by shipping zone and service level, using the Shipments and ShippingZones tables.
SELECT
	z.Code AS ZoneCode,
	s.ServiceLevel,
	COUNT(*) AS ShipmentCount,
	SUM(s.Price) AS TotalValue
FROM Shipments s
INNER JOIN ShippingZones z ON z.Id = s.ZoneId
GROUP BY z.Code, s.ServiceLevel
HAVING COUNT(*) > 0
ORDER BY TotalValue DESC, z.Code, s.ServiceLevel;
GO