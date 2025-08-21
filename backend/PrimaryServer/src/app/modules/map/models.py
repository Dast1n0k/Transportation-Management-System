from dataclasses import dataclass, asdict
from typing import Optional, Dict, Any


@dataclass
class ZipCodeInfo:
    """Модель информации о ZIP-коде"""
    zipcode: str
    city: Optional[str] = None
    state: Optional[str] = None
    county_name: Optional[str] = None
    country: str = "USA"
    lat: Optional[float] = None
    lon: Optional[float] = None

    def to_dict(self) -> Dict[str, Any]:
        return asdict(self)
