class AuthException(Exception):
    """Базовое исключение для модуля аутентификации"""

    def __init__(self, message, code=None, status_code=400):
        self.message = message
        self.code = code
        self.status_code = status_code
        super().__init__(self.message)

    def to_dict(self):
        result = {'error': self.message}
        if self.code:
            result['code'] = self.code
        return result


class ValidationError(AuthException):
    """Ошибка валидации данных"""

    def __init__(self, message, code="VALIDATION_ERROR"):
        super().__init__(message, code, 400)


class AuthenticationError(AuthException):
    """Ошибка аутентификации"""

    def __init__(self, message, code="AUTHENTICATION_ERROR"):
        super().__init__(message, code, 401)


class AuthorizationError(AuthException):
    """Ошибка авторизации"""

    def __init__(self, message, code="AUTHORIZATION_ERROR"):
        super().__init__(message, code, 403)


class UserNotFoundError(AuthException):
    """Пользователь не найден"""

    def __init__(self, message="User not found", code="USER_NOT_FOUND"):
        super().__init__(message, code, 404)


class UserExistsError(AuthException):
    """Пользователь уже существует"""

    def __init__(self, message="Username already exists", code="USERNAME_EXISTS"):
        super().__init__(message, code, 409)


class TokenError(AuthException):
    """Ошибка токена"""

    def __init__(self, message, code="TOKEN_ERROR"):
        super().__init__(message, code, 401)
