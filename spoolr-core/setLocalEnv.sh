#!/bin/bash
export $(cat .env | grep -v '^#' | grep -v '^$' | xargs)
