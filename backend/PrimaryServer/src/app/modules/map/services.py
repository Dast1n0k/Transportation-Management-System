import pgeocode
from .models import ZipCodeInfo


class ZipCodeService:
    def __init__(self):
        self._nomi = pgeocode.Nominatim('us')

    def get_zipcode_info(self, zipcode: str):
        result = self._nomi.query_postal_code(zipcode)

        return ZipCodeInfo(
            city=result.place_name,
            state=result.state_name,
            country="USA",
            lat=result.latitude,
            lon=result.longitude
        )
