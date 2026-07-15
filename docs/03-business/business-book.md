---
title: Domora Business Book
version: 1.0
status: Draft
owner: Domora Engineering Team
Last-updated: 2026-07-12
Time: 18:00 
---

# Domora Business Book

## Purpose

The Business Book describes how a property business operates.

It is the authoritative source for understanding Domora's business domain.

Every database table, API endpoint, workflow, AI agent, and user interface must reflect the business rules defined here.

Software implements this document.

It never replaces it.

---

## Principle

Nothing important is deleted. Everything progresses through a lifecycle.

---

## Don't think of Domora as:

 - "A property management application."

Think of it as:

 - A machine that observes, validates, records, and guides the lifecycle of a property business.

. Observe: Capture business events as they happen.
. Validate: Enforce the business rules before accepting them.
. Record: Preserve the truth and history permanently.
. Guide: Help users make the next correct business decision.

---

# 1. Introduction

## Why Domora Exists

Property businesses manage valuable physical assets, people, contracts, and money.

Many businesses still rely on notebooks, spreadsheets, WhatsApp conversations, and disconnected software.

This leads to:

- Lost information
- Missed payments
- Poor communication
- Operational inefficiency
- Lack of accountability

Domora exists to provide a single trusted platform where every property business operation is connected.

---

# 2. Understanding a Property Business

A property business exists to provide rentable spaces in exchange for recurring revenue.

Everything inside the business supports that objective.

The business continuously answers questions such as:

- Who owns this property?
- Which units are vacant?
- Who currently occupies this unit?
- Which invoices remain unpaid?
- Which payments have been received?
- Which leases expire soon?

Domora exists to answer these questions accurately.

---

# 3. The Core Business Model

The property business is built from several connected business entities.

Each entity represents something that exists in the real world.

Organization
    ↓
Property
    ↓
Unit
    ↓
Lease
    ↓
Invoice
    ↓
Payment

Supporting entities include:

Tenant

Users

Staff

Water Bills

Maintenance

Audit Logs

Documents

Notifications

Reports

Artificial Intelligence

---

# 4. Business Entities

Business entities represent real-world concepts.

They are not software features.

They would exist even if computers disappeared tomorrow.

## Organization

An Organization is the highest business entity inside Domora.

It represents the business responsible for managing one or more properties.

Examples include:

- Individual landlords
- Property management companies
- Real estate businesses
- Housing organizations

Responsibilities

- Own properties
- Manage staff
- Collect rent
- Track finances
- Define business policies

Business Rules

• Every Organization has exactly one Owner.

• An Organization may manage many Properties.

• Organizations cannot access each other's data.

• Every operation belongs to an Organization.

## Property

A Property is a physical location managed by an Organization.

Examples include:

- Apartment Complex

- Commercial Building

- Mixed-use Building

- Shopping Centre

Responsibilities

- Provide rentable units.

Business Rules

• A Property belongs to exactly one Organization.

• A Property contains one or more Units.

• A Property cannot exist without an Organization.

## Unit

A Unit is the smallest rentable space within a Property.

Examples

- Bedsitter

- One Bedroom

- Shop

- Office

- Warehouse

Current Status

- Vacant

- Reserved

- Occupied

- Maintenance

Business Rules

• Every Unit belongs to one Property.

• Only one Active Lease may occupy a Unit at a time.

• Units maintain historical occupancy through Leases.