<%- config[:constant_array].each_with_index do |c,i| -%>
<%= '  '*i %>module <%= c %>
<%- end -%>
<%= '  '*config[:constant_array].size %>VERSION = "0.0.1"
<%- (config[:constant_array].size-1).downto(0) do |i| -%>
<%= '  '*i %>end
<%- end -%>
