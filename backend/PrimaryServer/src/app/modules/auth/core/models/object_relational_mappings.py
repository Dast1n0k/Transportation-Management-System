from sqlalchemy import (
    func,
    Column,
    String,
    DateTime,
    CheckConstraint,
    text
)
from sqlalchemy.ext.declarative import declarative_base

Base = declarative_base()


class User(Base):
    __tablename__ = 'users'

    id = Column(
        String,
        primary_key=True,
        nullable=False,
        server_default=text("(lower(hex(randomblob(16))))")
    )

    username = Column(String, unique=True, nullable=False)
    password = Column(String, nullable=False)
    role = Column(String, nullable=False)

    # Fix the datetime defaults
    created_at = Column(
        DateTime,
        server_default=func.current_timestamp()
    )
    updated_at = Column(
        DateTime,
        onupdate=func.current_timestamp()
    )

    __table_args__ = (
        CheckConstraint(
            "role IN ('admin', 'manager', 'courier')",
            name='check_role'
        ),
    )
