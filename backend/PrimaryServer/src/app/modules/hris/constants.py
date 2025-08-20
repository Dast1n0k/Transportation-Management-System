# Типы транспорта
class VehicleTypes:
    SPRINTER = "sprinter"
    SMALL_STRAIGHT = "small_straight"
    LARGE_STRAIGHT = "large_straight"

    ALL_TYPES = [SPRINTER, SMALL_STRAIGHT, LARGE_STRAIGHT]

    # Описания типов транспорта
    DESCRIPTIONS = {
        SPRINTER: "Sprinter van",
        SMALL_STRAIGHT: "Small straight truck",
        LARGE_STRAIGHT: "Large straight truck"
    }


# Коды ошибок специфичные для модуля курьеров
class CourierErrorCodes:
    # Валидация
    VALIDATION_ERROR = "VALIDATION_ERROR"
    INVALID_VEHICLE_TYPE = "INVALID_VEHICLE_TYPE"
    INVALID_DIMENSIONS_FORMAT = "INVALID_DIMENSIONS_FORMAT"
    INVALID_PHONE_FORMAT = "INVALID_PHONE_FORMAT"
    INVALID_COORDINATES = "INVALID_COORDINATES"

    # Курьеры
    COURIER_NOT_FOUND = "COURIER_NOT_FOUND"
    COURIER_ALREADY_EXISTS = "COURIER_ALREADY_EXISTS"
    PHONE_ALREADY_EXISTS = "PHONE_ALREADY_EXISTS"
    USER_ALREADY_HAS_COURIER_PROFILE = "USER_ALREADY_HAS_COURIER_PROFILE"

    # Права доступа
    ACCESS_DENIED = "ACCESS_DENIED"
    CANNOT_MODIFY_OTHER_COURIER = "CANNOT_MODIFY_OTHER_COURIER"


# Валидационные правила
class CourierValidationRules:
    MIN_NAME_LENGTH = 0
    MAX_NAME_LENGTH = 50
    MIN_PHONE_LENGTH = 0
    MAX_PHONE_LENGTH = 15
    MAX_NOTES_LENGTH = 500
    MAX_LOCATION_LENGTH = 500
    MAX_ZIPCODE_LENGTH = 10

    # Диапазоны координат
    MIN_LATITUDE = -90.0
    MAX_LATITUDE = 90.0
    MIN_LONGITUDE = -180.0
    MAX_LONGITUDE = 180.0

    # Форматы размеров
    DIMENSIONS_2D_PATTERN = r'^\d+\*\d+$'  # Например: "92*71"
    DIMENSIONS_3D_PATTERN = r'^\d+\*\d+\*\d+$'  # Например: "152*94*72"


# Настройки поиска (изменено с км на мили)
class SearchSettings:
    DEFAULT_SEARCH_RADIUS_MILES = 100
    MAX_SEARCH_RADIUS_MILES = 600
    DEFAULT_LIMIT = 50
    MAX_LIMIT = 100


# Статусы доступности
class AvailabilityStatus:
    AVAILABLE = True
    UNAVAILABLE = False

    STATUS_LABELS = {
        True: "Available",
        False: "Unavailable"
    }
