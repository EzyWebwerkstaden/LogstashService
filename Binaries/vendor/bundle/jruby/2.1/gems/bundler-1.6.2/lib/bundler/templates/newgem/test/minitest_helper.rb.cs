$LOAD_PATH.unshift File.expand_path('../../lib', __FILE__)
require '<%= config[:namespaced_path] %>'

require 'minitest/autorun'
